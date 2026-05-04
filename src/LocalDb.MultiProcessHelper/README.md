# Multi-process race reproducers

This folder is a regression-test scaffold, not a shipped artifact. It exists to deterministically reproduce a class of races in `Wrapper.InnerStart` that surface when multiple OS processes share a single LocalDB user instance.

## Symptom

When two test-host processes (e.g. two `dotnet test` invocations, or Rider's runner concurrent with a CLI run) target the same `SqlInstance<T>` for the same Windows user, intermittent failures appear with stack traces like:

```
SetUp : Microsoft.Data.SqlClient.SqlException :
  A network-related or instance-specific error occurred while establishing a connection to SQL Server.
  ... error: 50 - Local Database Runtime error occurred.
  The specified LocalDB instance does not exist.
  ----> System.ComponentModel.Win32Exception : Unknown error (0x89c50107)
   at Wrapper.OpenMasterConnection() in C:\projects\localdb\src\LocalDb\Wrapper.cs:line 274
   at Wrapper.CreateAndDetachTemplate(...) in ...:line 229
   at Wrapper.CreateDatabaseFromTemplate(String name) in ...:line 83
   at EfLocalDb.SqlInstance`1.Build(String, IEnumerable`1) in ...:line 73
   at EfLocalDbNunit.LocalDbTestBase`1.Reset() in ...:line 85
```

The `0x89C50107` native code is `LOCALDB_ERROR_INSTANCE_DOES_NOT_EXIST`. Other manifestations of the same underlying race include SQL deadlocks during `CREATE DATABASE [template]` and `Operating system error 2: cannot find the file specified` on `template.mdf`.

Once a machine is in this state it tends to stay broken: every subsequent `dotnet test` triggers the same race because the wrapper directory is empty (no `template.mdf`), so every process re-runs the destructive `StopAndDelete + CleanStart` branch.

## Root cause

`Wrapper.InnerStart` (LocalDb.csproj, `Wrapper.cs`):

```csharp
var info = LocalDbApi.GetInstance(instance);
if (!info.Exists)         { CleanStart(); return; }
if (!info.IsRunning)      { LocalDbApi.StartInstance(instance); }
if (!File.Exists(DataFile))
{
    LocalDbApi.StopAndDelete(instance);
    CleanStart();   // CreateInstance + StartInstance + CreateAndDetachTemplate
    return;
}
```

There are two unsynchronized concurrency surfaces here:

1. **In-process** — `Wrapper.semaphoreSlim` is declared but never `WaitAsync`'d; two `Wrapper` instances for the same instance name running in the same process race on `LocalDbApi.*` calls and on the SQL DDL inside `CreateAndDetachTemplate`.
2. **Cross-process** — even if (1) were fixed with an in-process lock, the `LocalDbApi.*` calls reach into the per-Windows-user LocalDB metadata, which is shared across all processes belonging to that user. Two processes both running `InnerStart` against the same instance race on `StopAndDelete` / `CreateInstance` / `StartInstance` and on the same master DB.

Both surfaces dissolve under one fix: serialize `InnerStart` per instance name with an in-process lock **and** a named cross-process mutex.

## The three reproducer tests

| Test | Race surface | Failure surfaced |
|---|---|---|
| `ConcurrentStartTests.ConcurrentStartWithMissingTemplateShouldNotRace` | In-process (two `Wrapper` instances, one process, no helper exe) | SQL deadlock 1205 during `CREATE DATABASE [template]` |
| `MultiProcessConcurrentStartTests.MultiProcessConcurrentStartShouldNotRace` | Multi-process, symmetric (3 child processes all running `Wrapper.Start`) | SQL deadlock OR `template.mdf` not found OR `0x89C50107` (varies by timing) |
| `InstanceDoesNotExistRaceTests.KillerVsVictimSurfacesInstanceDoesNotExist` | Multi-process, asymmetric (one killer hammering `StopAndDelete`, one victim opening `SqlConnection`) | **Exact `0x89C50107` deterministically** — victim only exits 0 when it observes that specific code |

## Why each part exists

### `LocalDb.MultiProcessHelper` project

The asymmetric/multi-process tests need to spawn separate Windows processes via `Process.Start`. A Windows process needs an executable; an executable needs an entry point; that entry point lives in `Program.cs`.

We can't reuse `LocalDb.Tests.exe` for this — its entry point is owned by the test runner (NUnit + Microsoft.Testing.Platform), and we'd have to either fight the runner or invoke `dotnet test --filter` recursively (slow and awkward). A purpose-built console exe is simpler and faster.

### `Program.cs` with three modes (`wrapper-start`, `killer`, `victim`)

Different tests need different child behaviors. Rather than ship three executables, the same exe takes a mode argument:

- **`wrapper-start`** — full `Wrapper.Start` cycle. Used by the symmetric multi-process test where every child runs the same code path.
- **`killer`** — bare `LocalDbApi.StopAndDelete(name)` in a tight loop. Maximizes the chance of catching a victim mid-handshake.
- **`victim`** — `SqlConnection.OpenAsync` in a tight loop, walking exception chains for `Win32Exception.NativeErrorCode == 0x89C50107`. Exits 0 the first time it observes that exact code, exits 1/2 otherwise.

Splitting the killer and victim into separate processes is what makes `0x89C50107` reliably reproducible — symmetric children all running `Wrapper.Start` race on multiple things at once and surface a mix of error types; the asymmetric setup isolates the specific race window where the LocalDB API resolves the instance name as "does not exist."

### Strong-name signing (`SignAssembly` + `..\key.snk` in the .csproj)

`Wrapper`, `LocalDbApi`, and `DirectoryFinder` are `internal` types in the LocalDb assembly. The LocalDb assembly is strong-named and grants `InternalsVisibleTo` only to assemblies whose public key matches a specific `PublicKey=...` blob. For the helper to use those internal types, it must be signed with the same key. `..\key.snk` is the existing project-wide signing key (the same one Benchmark uses).

Alternative considered: drive the race entirely through `EfLocalDb.SqlInstance<T>` (a public API). That works but requires defining a `DbContext` and adds EF Core to the helper's dependency surface. Reaching for `Wrapper` directly keeps the helper minimal and exercises exactly the layer where the race lives.

### `InternalsVisibleTo` entry for `LocalDb.MultiProcessHelper`

Standard IVT plumbing — added next to the existing entries in `src/LocalDb/InternalsVisibleTo.cs`. Same `PublicKey=` blob as the others (it's the public half of `key.snk`).

### `<ProjectReference ... ReferenceOutputAssembly="false" Private="false" />` in `LocalDb.Tests.csproj`

The test project does **not** want to link the helper's assembly into its own output — it only wants the helper exe to exist on disk before tests run. `ReferenceOutputAssembly="false"` says "build it, but don't add a reference to its DLL in my compile inputs." `Private="false"` says "don't copy its outputs into my bin folder." With both set, the helper builds whenever the test project does (so a fresh `dotnet test` always finds an up-to-date helper), but there's no compile-time coupling between them.

The test resolves the helper path at runtime via `HelperExeResolver.cs`, which walks up from the test's `bin/<Config>/net10.0/` to find the sibling project's matching `bin/<Config>/net10.0/LocalDb.MultiProcessHelper.exe`.

### `LocalDb.slnx` entry

Nothing surprising — registers the new project so tooling (Rider, Visual Studio, `dotnet sln` operations) sees it. Without this the project still builds via the test project's `ProjectReference`, but it won't appear in solution-level views.

### Signal-file barrier (`signalFile` argument)

`Process.Start` spin-up jitter is on the order of 100–300 ms — wider than the actual race window for `0x89C50107`, which is microseconds. If children just started running their work immediately, the slowest child would always lose the race in a predictable order, and the test would be flaky.

The barrier flips this around: each child spawns, waits in a polling loop for a signal file to appear, and only proceeds once the parent test creates that file. The parent waits 750 ms after spawning all children (giving them enough time to load their CLR and reach the wait loop), then writes the signal — releasing them within a few ms of each other. That's tight enough to land the children in the actual race window reliably.

### `HelperExeResolver` (shared lookup)

Both multi-process tests need to find the helper exe at runtime, and the path resolution is non-trivial enough to want one place to update if the build layout changes. Pulling it out also avoids duplicate logic that could drift between the two tests.

## Fix applied in this PR

`Wrapper.cs` now serializes `Start` per instance name across two layers:

* **In-process** — a static `ConcurrentDictionary<string, SemaphoreSlim>` keyed by instance name. Two `Wrapper` instances in one process for the same LocalDB instance now share a semaphore, replacing the per-instance `SemaphoreSlim` field that was declared but never used.
* **Cross-process** — a sentinel file in `%TEMP%\localdb_wrapper_<instanceName>.lock` opened with `FileShare.None`. File handles aren't thread-affine (unlike `Mutex`), so the handle can be acquired in `Start` and released asynchronously in a continuation chained onto `startupTask`.

Both locks are acquired before `InnerStart` and released only after `startupTask` completes — that span covers both the synchronous `LocalDbApi.*` calls and the async `CreateAndDetachTemplate` master-DB DDL.

After the fix:

* `ConcurrentStartTests.ConcurrentStartWithMissingTemplateShouldNotRace` — passes. Second `Wrapper.Start` in the same process now waits for the first to finish, takes the fast no-rebuild path on its second turn (the first run created the template).
* `MultiProcessConcurrentStartTests.MultiProcessConcurrentStartShouldNotRace` — passes. Subsequent processes block on the file lock and pick up the template the first process produced.
* `InstanceDoesNotExistRaceTests.KillerVsVictimSurfacesInstanceDoesNotExist` — still passes. This test bypasses `Wrapper` and exercises `LocalDbApi.StopAndDelete` directly against `SqlConnection.Open`, so the fix does not (and should not) change its behaviour — it stays in the suite as documentation of the underlying OS-level race that `Wrapper`'s lock now protects callers from.

The existing `WrapperTests` suite continues to pass unchanged.

## Running the tests

```powershell
dotnet test src/LocalDb.Tests/LocalDb.Tests.csproj `
    --configuration Release `
    --filter "FullyQualifiedName~ConcurrentStart|FullyQualifiedName~MultiProcessConcurrentStart|FullyQualifiedName~KillerVsVictim"
```

The deterministic `KillerVsVictimSurfacesInstanceDoesNotExist` finishes in ~8 s. The symmetric `MultiProcessConcurrentStartShouldNotRace` finishes in ~15-30 s. The in-process `ConcurrentStartWithMissingTemplateShouldNotRace` finishes in ~2 minutes (it intentionally rebuilds the template 5× for a non-flaky signal).
