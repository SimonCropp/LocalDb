# Design

There is a tiered approach to the API.

For SQL:

SqlInstance > SqlDatabase > Connection

For EF:

SqlInstance > SqlDatabase > EfContext

SqlInstance represents a [SQL Sever instance](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/database-engine-instances-sql-server?#instances) (in this case hosted in SqlLocalDB) and SqlDatabase represents a [SQL Sever Database](https://docs.microsoft.com/en-us/sql/relational-databases/databases/databases?view=sql-server-2017) running inside that SqlInstance.

From a API perspective:

For SQL:

`SqlInstance` > `SqlDatabase` > `SqlConnection`

For EF:

`SqlInstance<TDbContext>` > `SqlDatabase<TDbContext>` > `TDbContext`


Multiple SqlDatabases can exist inside each SqlInstance. Multiple DbContexts/SqlConnections can be created to talk to a SqlDatabase.

On the file system, each SqlInstance has corresponding directory and each SqlDatabase has a uniquely named mdf file within that directory.

When a SqlInstance is defined, a template database is created. All subsequent SqlDatabases created from that SqlInstance will be based on this template. The template allows schema and data to be created once, instead of every time a SqlDatabase is required. This results in improved performance by not requiring to re-create/re-migrate the SqlDatabase schema/data on each use.

The usual approach for consuming the API in a test project is as follows.

 * Single SqlInstance per test project.
 * Single SqlDatabase per test (or instance of a parameterized test).
 * One or more DbContexts/SqlConnections used within a test.

This assumes that there is a schema and data (and DbContext in the EF context) used for all tests. If those caveats are not correct then multiple SqlInstances can be used.