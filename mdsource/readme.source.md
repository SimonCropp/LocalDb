# LocalDb

Provides a wrapper around [LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) to simplify running tests against [Entity Framework](https://docs.microsoft.com/en-us/ef/core/) or a raw SQL Database.


## More info

 * [Design](/pages/design.md)
 * [Raw SqlConnection Usage](/pages/raw-usage.md)
 * [EntityFramework Usage](/pages/ef-usage.md)
 * [EntityFramework Migrations](/pages/efmigrations.md)
 * [Directory and instance name resolution](/pages/directory-and-instance-name-resolution.md)


## Why


### Goals:

 * Have a isolated SQL Server Database for each unit test method.
 * Does not overly impact performance.
 * Results in a running SQL Server Database that can be accessed via [SQL Server Management Studio ](https://docs.microsoft.com/en-us/sql/ssms/sql-server-management-studio-ssms?view=sql-server-2017) (or other tooling) to diagnose issues when a test fails.


### Why not SQLite

 * SQLite and SQL Server do not have compatible feature sets and there are [incompatibilities between their query languages](https://www.mssqltips.com/sqlservertip/4777/comparing-some-differences-of-sql-server-to-sqlite/).


### Why not SQL Express or full SQL Server

 * Control over file location. LocalDB connections support AttachDbFileName property, which allows developers to specify a database file location. LocalDB will attach the specified database file and the connection will be made to it. This allows database files to be stored in a temporary location, and cleaned up, as required by tests.
 * No installed service is required. Processes are started and stopped automatically when needed.
 * Automatic cleanup. A few minutes after the last connection to this process is closed the process shuts down.
 * Full control of instances using the [Command-Line Management Tool: SqlLocalDB.exe](https://docs.microsoft.com/en-us/sql/relational-databases/express-localdb-instance-apis/command-line-management-tool-sqllocaldb-exe?view=sql-server-2017).


### Why not [EF InMemory](https://docs.microsoft.com/en-us/ef/core/providers/in-memory/)

 * Difficult to debug the state. When debugging a test, or looking at the resultant state, it is helpful to be able to interrogate the Database using tooling
 * InMemory is implemented with shared mutable state between instance. This results in strange behaviors when running tests in parallel, for example when [creating keys](https://github.com/aspnet/EntityFrameworkCore/issues/6872).
 * InMemory is not intended to be an alternative to SqlServer, and as such it does not support the full suite of SqlServer features. For example:
    * Does not support [Timestamp/row version](https://docs.microsoft.com/en-us/ef/core/modeling/concurrency#timestamprow-version).
    * [Does not validate constraints](https://github.com/aspnet/EntityFrameworkCore/issues/2166).

See the official guidance: [InMemory is not a relational database](https://docs.microsoft.com/en-us/ef/core/miscellaneous/testing/in-memory#inmemory-is-not-a-relational-database).


## References:

 * [Which Edition of SQL Server is Best for Development Work?](https://www.red-gate.com/simple-talk/sql/sql-development/edition-sql-server-best-development-work/#8)
 * [Introducing LocalDB, an improved SQL Express](https://blogs.msdn.microsoft.com/sqlexpress/2011/07/12/introducing-localdb-an-improved-sql-express/)


## Usage

This project currently supports two approaches.


### Raw SqlConnection

Interactions with LocalDB via a [SqlConnection](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlconnection).

[Full Usage](/pages/raw-usage.md)


### EntityFramework

Interactions with LocalDB via [Entity Framework](https://docs.microsoft.com/en-us/ef/core/).

[Full Usage](/pages/ef-usage.md)


## Debugging

To connect to a LocalDB instance using [SQL Server Management Studio ](https://docs.microsoft.com/en-us/sql/ssms/sql-server-management-studio-ssms?view=sql-server-2017) use a server name with the following convention `(LocalDb)\INSTANCENAME`.

So for a DbContext named `MyDbContext` the server name would be `(LocalDb)\MyDbContext`. Note that the name will be different if a `name` or `instanceSuffix` have been defined for SqlInstance.

The server name will be written to [Trace.WriteLine](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.trace.writeline) when a SqlInstance is constructed. It can be accessed programmatically from `SqlInstanceService.ServerName` or `SqlInstance.ServerName`.


## SqlLocalDb

The [SqlLocalDb Utility (SqlLocalDB.exe)](https://docs.microsoft.com/en-us/sql/tools/sqllocaldb-utility) is a command line tool to enable users and developers to create and manage an instance of LocalDB.

Useful commands:

 * `sqllocaldb info`: list all instances
 * `sqllocaldb create InstanceName`: create a new instance
 * `sqllocaldb start InstanceName`: start an instance
 * `sqllocaldb stop InstanceName`: stop an instance
 * `sqllocaldb delete InstanceName`: delete an instance (this does not delete the file system data for the instance)


## SQL Server Updates

Ensure that the latests SQL Server Cumulative Update is being used.

 * [SQL Server 2017](https://support.microsoft.com/en-au/help/4047329/sql-server-2017-build-versions)


## Simple.LocalDb

LocalDB API code sourced from https://github.com/skyguy94/Simple.LocalDb


## Icon

[Robot](https://thenounproject.com/term/robot/960055/) designed by [Creaticca Creative Agency](https://thenounproject.com/creaticca/) from [The Noun Project](https://thenounproject.com/).