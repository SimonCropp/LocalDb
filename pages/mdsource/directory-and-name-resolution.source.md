# SqlLocalDB Directory and Instance Name Resolution

The instance name is defined as:

snippet: GetInstanceName

That InstanceName is then used to derive the data directory. In order:

 * If `LocalDBData` environment variable exists then use `LocalDBData\InstanceName`.
 * If `AGENT_TEMPDIRECTORY` environment variable exists then use `AGENT_TEMPDIRECTORY\LocalDb\InstanceName`.
 * Use `%TempDir%\LocalDb\InstanceName`.

There is an explicit registration override that takes an instance name and a directory for that instance:


## For SQL:

snippet: ExplicitName


## For EntityFramework:

snippet: EfExplicitName


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

snippet: EFWithDbName