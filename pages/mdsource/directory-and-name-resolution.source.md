# SqlLocalDB Directory and Instance Name Resolution

The instance name is defined as:

snippet: GetInstanceName

That InstanceName is then used to derive the data directory. In order:

 * If `LocalDBData` environment variable exists then use `LocalDBData\InstanceName`.
 * Use `%Temp%\LocalDb\InstanceName`.

Instances that have not been written to for six hours are purged: the database files are deleted from the data directory, and the LocalDB instance and the directory LocalDB keeps for it are removed.

There is an explicit registration override that takes an instance name and a directory for that instance:


## For SQL:

snippet: ExplicitName


## For EntityFramework:

snippet: EfExplicitName


## The LocalDB instance directory

As well as the data directory above, LocalDB keeps a directory of its own for each instance at `%LocalAppData%\Microsoft\Microsoft SQL Server Local DB\Instances\InstanceName`. It holds the system databases (`master`, `model`, `msdb` and `tempdb`), the error logs, and the extended event files.

That location is owned by LocalDB and cannot be changed. It is derived from the local application data folder, the API that creates an instance takes no path, and the `LocalDBData` environment variable does not apply to it.

Deleting an instance reclaims the system databases but leaves the logs and event files behind, so purging an instance removes both the instance and this directory.

The per run purge above only sees instances that still have a data directory. Once that directory is gone, cleared with the temp directory or by the purge itself, nothing under the data root points at the instance and it can no longer be found that way. To catch these, this directory is also swept.

LocalDB instances are shared by everything on the machine that uses LocalDB, and nothing in an instance records what created it. So a marker file is written into the directory of each instance this library starts, and the sweep only ever reclaims marked instances. An instance created by anything else has no marker and is never removed. A marked instance is reclaimed once it has no data directory, is not running, and has been untouched for a threshold defaulting to 30 days. Configurable via the `LocalDBInstanceCleanupDays` environment variable, or `LocalDbSettings.InstanceCleanupThreshold`; set it to zero to disable the sweep.

Instances that predate marking cannot be told apart from instances belonging to anything else. They get a single best effort pass, once per machine, which removes directories left behind by already deleted instances and orphaned instances that still carry the shrunk `model.mdf` this library leaves behind. Anything else is left alone, since there is no way to tell it apart from an instance belonging to something else, and is left for `SqlLocalDB.exe delete` to remove by hand. That the pass has run is recorded under `%LocalAppData%\LocalDb`.


## Virus scanning exclusions

Both directories hold database files that are written to constantly while tests run: template files are copied for every database built, and instances are created, attached, and detached throughout a run. Real time virus scanning of that activity is a well known drag on SQL Server performance, so on development and build machines consider excluding both:

snippet: Set-LocalDb-AV-Exclusions.ps1

Adding an exclusion needs elevation, so the script raises a UAC prompt if it is not already running as administrator. The paths are resolved before elevating, since elevation can switch to an account with a different profile.

Note that excluding a path is a trade off against the protection it provides, and on a managed machine it is usually controlled by policy rather than being the developer's decision.


### In CI

Hosted agents need nothing. GitHub hosted runners and Azure Pipelines Microsoft hosted agents are built from the same [images](https://github.com/actions/runner-images), and that build [turns off real time monitoring](https://github.com/actions/runner-images/blob/main/images/windows/scripts/build/Configure-WindowsDefender.ps1) along with behavior monitoring, script scanning and scheduled scans. There is nothing left to exclude from.

Self hosted agents are worth excluding, as part of provisioning the machine rather than as a build step. Two things differ from running the script on a developer machine:

 * The agent usually runs as a service account with no interactive desktop, so the UAC prompt cannot be raised. Run the script from an already elevated shell.
 * The directories belong to the profile of the account the agent runs as. When provisioning from a different account, pass them explicitly:

```ps1
.\Set-LocalDb-AV-Exclusions.ps1 `
    -dataRoot 'C:\Users\svc-agent\AppData\Local\Temp\LocalDb' `
    -instanceRoot 'C:\Users\svc-agent\AppData\Local\Microsoft\Microsoft SQL Server Local DB\Instances'
```


## Building using Azure machines

When using azure hosted machines for build agents, it makes sense to use the agent temp directory as defined by the `AGENT_TEMPDIRECTORY` environment variable. The reason for this is that the temp directory is located on a secondary drive. However this drive has some strange permissions that will cause run time errors, usually manifesting as a SqlException with `Could not open new database...`. To work around this run the following script at machine startup:

snippet: Set-D-Drive-Permissions.ps1


# Database Name Resolution

A design goal is to have an isolated database per test. To facilitate this the `SqlInstance.Build` method has a convention based approach. It contains the following parameters:

 * `testFile`: defaults to the full path of the source file that contains the caller via [CallerFilePathAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.callerfilepathattribute).
 * `memberName`: defaults to the method name of the caller to the method via [CallerMemberName](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.callermembername).
 * `databaseSuffix`: an optional parameter to further uniquely a database name when the `testFile` and `memberName` are not sufficient. For example when using parameterized tests.

The convention signature is as follows:

snippet: ConventionBuildSignature

With these parameters the database name is the derived as follows:

snippet: DeriveName


## Explicit name

If full control over the database name is required, there is an overload that takes an explicit name:

snippet: ExplicitBuildSignature

Which can be used as follows:


### For SQL:

snippet: WithDbName


### For EntityFramework:

snippet: EfWithDbName