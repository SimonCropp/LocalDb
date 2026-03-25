# Raw SQL Usage

Interactions with SqlLocalDB via a [SqlConnection](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlconnection).


## LocalDb package [![NuGet Status](https://img.shields.io/nuget/v/LocalDb.svg)](https://www.nuget.org/packages/LocalDb/)

https://nuget.org/packages/LocalDb/


## Schema and data

The snippets use the following helper class:

snippet: TestDbBuilder.cs


## Initialize SqlInstance

SqlInstance needs to be initialized once.

To ensure this happens only once there are several approaches that can be used:


### Static constructor

In the static constructor of a test.

If all tests that need to use the SqlInstance existing in the same test class, then the SqlInstance can be initialized in the static constructor of that test class.

snippet: StaticConstructor


### Static constructor in test base

If multiple tests need to use the SqlInstance, then the SqlInstance should be initialized in the static constructor of test base class.

snippet: TestBase


## Anti-patterns

### Do not call Build once and share the ConnectionString

A common mistake is to call `Build()` once (e.g. in a static constructor) and reuse the resulting `ConnectionString` across all tests:

```cs
// WRONG: defeats per-test isolation
static Connection()
{
    var database = sqlInstance.Build("shared").GetAwaiter().GetResult();
    ConnectionString = database.ConnectionString;
}

// All tests share the same database
public static SqlConnection OpenConnection() => new(ConnectionString);
```

This defeats the purpose of LocalDb's template cloning. All tests share the same database, requiring manual cleanup (`DELETE FROM ...`) between tests, preventing parallel execution, and risking test interference.

Instead, call `Build()` in each test method to get an isolated database clone:

```cs
// CORRECT: each test gets its own database
[Test]
public async Task MyTest()
{
    await using var database = await sqlInstance.Build();
    var connection = database.Connection;
    // connection points to a fresh database cloned from the template
}
```


## Usage in a Test

Usage inside a test consists of two parts:


### Build a SqlDatabase

snippet: BuildDatabase

See: [Database Name Resolution](/pages/directory-and-name-resolution.md#database-name-resolution)


### Using SQLConnection

snippet: BuildContext


### Full Test

The above are combined in a full test:

snippet: SnippetTests


## Shared Database

`BuildShared` creates a single database from the template once and reuses it across calls. This is useful for query-only tests that don't need per-test isolation.

snippet: SharedDatabase

Pass `useTransaction: true` to get an auto-rolling-back transaction, allowing writes without affecting other tests.

Note: `useTransaction: true` means that on test failure the resulting database cannot be inspected (since the transaction is rolled back). A workaround when debugging a failure is to temporarily remove `useTransaction: true`.

snippet: SharedDatabase_WithTransaction