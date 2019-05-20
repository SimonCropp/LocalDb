<!--
This file was generate by MarkdownSnippets.
Source File: /mdsource/readme.source.md
To change this file edit the source file and then re-run the generation using either the dotnet global tool (https://github.com/SimonCropp/MarkdownSnippets#markdownsnippetstool) or using the api (https://github.com/SimonCropp/MarkdownSnippets#running-as-a-unit-test).
-->
# LocalDb

Provides a wrapper around [LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) to simplify running tests against [Entity Framework](https://docs.microsoft.com/en-us/ef/core/) or a raw SQL Database.


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


## References:

 * [Which Edition of SQL Server is Best for Development Work?](https://www.red-gate.com/simple-talk/sql/sql-development/edition-sql-server-best-development-work/#8)
 * [Introducing LocalDB, an improved SQL Express](https://blogs.msdn.microsoft.com/sqlexpress/2011/07/12/introducing-localdb-an-improved-sql-express/)


## The NuGet packages

This project currently supports two approaches.


### 1. LocalDb package [![NuGet Status](http://img.shields.io/nuget/v/LocalDb.svg)](https://www.nuget.org/packages/LocalDb/)

Interactions with LocalDB via a [SqlConnection](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlconnection).

https://nuget.org/packages/LocalDb/

    PM> Install-Package LocalDb


### 2. EfLocalDb package [![NuGet Status](http://img.shields.io/nuget/v/EfLocalDb.svg)](https://www.nuget.org/packages/EfLocalDb/)

Interactions with LocalDB via [Entity Framework](https://docs.microsoft.com/en-us/ef/core/).

https://nuget.org/packages/EfLocalDb/

    PM> Install-Package EfLocalDb


## Design

There is a tiered approach to the API.

For SQL:

SqlInstance > SqlDatabase > Connection

For EF:

SqlInstance > SqlDatabase > EfContext

SqlInstance represents a [SQL Sever instance](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/database-engine-instances-sql-server?#instances) (in this case hosted in LocalDB) and SqlDatabase represents a [SQL Sever Database](https://docs.microsoft.com/en-us/sql/relational-databases/databases/databases?view=sql-server-2017) running inside that SqlInstance.

From a API perspective:

For SQL:

`SqlInstance` > `SqlDatabase` > `SqlConnection`

For EF:

`SqlInstance<TDbContext>` > `SqlDatabase<TDbContext>` > `TDbContext`


Multiple SqlDatabases can exist inside each SqlInstance. Multiple DbContexts/SqlConnections can be created to talk to a SqlDatabase.

On the file system, each SqlInstance has corresponding directory and each SqlDatabase has a uniquely named mdf file within that directory.

When a SqlInstance is defined, a template database is created. All subsequent SqlDatabases created from that SqlInstance will be based on this template. The template allows schema and data to be created once, instead of every time a SqlDatabase is required. This results in significant performance improvement by not requiring to re-create/re-migrate the SqlDatabase schema/data on each use.

The usual approach for consuming the API in a test project is as follows.

 * Single SqlInstance per test project.
 * Single SqlDatabase per test (or instance of a parameterized test).
 * One or more DbContexts/SqlConnections used within a test.

This assumes that there is a schema and data (and DbContext in the EF context) used for all tests. If those caveats are not correct then multiple SqlInstances can be used.

As the most common usage scenario is "Single SqlInstance per test project" there is a simplified static API to support it. To take this approach use `EFLocalDb.SqlInstanceService<TDbContext>` or `LocalDb.SqlInstanceService`.


## Usage


### Schema and data used in snippets


#### SQL

The SQL snippets use the following helper class:

<!-- snippet: TestDbBuilder.cs -->
```cs
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

public class TestDbBuilder
{
    public static void CreateTable(SqlConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "create table MyTable (Value int);";
            command.ExecuteNonQuery();
        }
    }

    public static async Task AddData(SqlConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
insert into MyTable (Value)
values (1);";
            await command.ExecuteNonQueryAsync();
        }
    }

    public static async Task<List<int>> GetData(SqlConnection connection)
    {
        var values = new List<int>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "select Value from MyTable";
            var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                values.Add(reader.GetInt32(0));
            }
        }
        return values;
    }
}
```
<sup>[snippet source](/src/LocalDb.Tests/TestDbBuilder.cs#L1-L41)</sup>
<!-- endsnippet -->


#### EF

The EF snippets use a DbContext of the following form:

<!-- snippet: TheDbContext.cs -->
```cs
using Microsoft.EntityFrameworkCore;

public class TheDbContext :
    DbContext
{
    public DbSet<TheEntity> TestEntities { get; set; }

    public TheDbContext(DbContextOptions options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<TheEntity>();
    }
}
```
<sup>[snippet source](/src/EfLocalDbSnippets/TheDbContext.cs#L1-L17)</sup>
<!-- endsnippet -->

<!-- snippet: TheEntity.cs -->
```cs
public class TheEntity
{
    public int Id { get; set; }
    public string Property { get; set; }
}
```
<sup>[snippet source](/src/EfLocalDbSnippets/TheEntity.cs#L1-L5)</sup>
<!-- endsnippet -->


### Initialize SqlInstance

SqlInstance needs to be initialized once.

To ensure this happens only once there are several approaches that can be used:


#### Static constructor

In the static constructor of a test.

If all tests that need to use the SqlInstance existing in the same test class, then the SqlInstance can be initialized in the static constructor of that test class.


##### For SQL:

<!-- snippet: StaticConstructor -->
```cs
public class Tests
{
    static SqlInstance sqlInstance;

    static Tests()
    {
        sqlInstance = new SqlInstance(
            name: "StaticConstructorInstance",
            buildTemplate: TestDbBuilder.CreateTable);
    }

    [Fact]
    public async Task Test()
    {
        var database = await sqlInstance.Build();
        using (var connection = await database.OpenConnection())
        {
            await TestDbBuilder.AddData(connection);
        }

        using (var connection = await database.OpenConnection())
        {
            Assert.Single(await TestDbBuilder.GetData(connection));
        }
    }
}
```
<sup>[snippet source](/src/LocalDbSnippets/StaticConstructor.cs#L7-L36)</sup>
<!-- endsnippet -->


##### For EF:

<!-- snippet: EfStaticConstructor -->
```cs
public class Tests
{
    static SqlInstance<DbContextUsedInStatic> sqlInstance;

    static Tests()
    {
        sqlInstance = new SqlInstance<DbContextUsedInStatic>(
            buildTemplate: (connection, builder) =>
            {
                using (var dbContext = new DbContextUsedInStatic(builder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            constructInstance: builder => new DbContextUsedInStatic(builder.Options));
    }

    [Fact]
    public async Task Test()
    {
        var database = await sqlInstance.Build();
        using (var dbContext = database.NewDbContext())
        {
            var entity = new TheEntity
            {
                Property = "prop"
            };
            dbContext.Add(entity);
            dbContext.SaveChanges();
        }

        using (var dbContext = database.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }
    }
}
```
<sup>[snippet source](/src/EfLocalDbSnippets/StaticConstructor.cs#L7-L47)</sup>
<!-- endsnippet -->


#### Static constructor in test base

If multiple tests need to use the SqlInstance, then the SqlInstance should be initialized in the static constructor of test base class.


##### For SQL:

<!-- snippet: TestBase -->
```cs
public class TestBase
{
    static SqlInstance instance;

    static TestBase()
    {
        instance = new SqlInstance(
            name:"TestBaseUsage",
            buildTemplate: TestDbBuilder.CreateTable);
    }

    public Task<SqlDatabase> LocalDb(
        string databaseSuffix = null,
        [CallerMemberName] string memberName = null)
    {
        return instance.Build(GetType().Name, databaseSuffix, memberName);
    }
}

public class Tests:
    TestBase
{
    [Fact]
    public async Task Test()
    {
        var database = await LocalDb();
        using (var connection = await database.OpenConnection())
        {
            await TestDbBuilder.AddData(connection);
        }
        
        using (var connection = await database.OpenConnection())
        {
            Assert.Single(await TestDbBuilder.GetData(connection));
        }
    }
}
```
<sup>[snippet source](/src/LocalDbSnippets/TestBaseUsage.cs#L8-L48)</sup>
<!-- endsnippet -->


##### For EF:

<!-- snippet: EfTestBase -->
```cs
public class TestBase
{
    static SqlInstance<TheDbContext> instance;

    static TestBase()
    {
        instance = new SqlInstance<TheDbContext>(
            buildTemplate: (connection, builder) =>
            {
                using (var dbContext = new TheDbContext(builder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            constructInstance: builder => new TheDbContext(builder.Options));
    }

    public Task<SqlDatabase<TheDbContext>> LocalDb(
        string databaseSuffix = null,
        [CallerMemberName] string memberName = null)
    {
        return instance.Build(GetType().Name, databaseSuffix, memberName);
    }
}

public class Tests:
    TestBase
{
    [Fact]
    public async Task Test()
    {
        var database = await LocalDb();
        using (var dbContext = database.NewDbContext())
        {
            var entity = new TheEntity
            {
                Property = "prop"
            };
            dbContext.Add(entity);
            dbContext.SaveChanges();
        }

        using (var dbContext = database.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }
    }
}
```
<sup>[snippet source](/src/EfLocalDbSnippets/TestBaseUsage.cs#L8-L59)</sup>
<!-- endsnippet -->


#### Module initializer

An alternative to the above "test base" scenario is to use a module initializer. This can be achieved using the [Fody](https://github.com/Fody/Home) addin [ModuleInit](https://github.com/Fody/ModuleInit):


##### For SQL:

<!-- snippet: ModuleInitializer -->
```cs
static class ModuleInitializer
{
    public static void Initialize()
    {
        SqlInstanceService.Register(
            name: "MySqlInstance",
            buildTemplate: TestDbBuilder.CreateTable);
    }
}
```
<sup>[snippet source](/src/LocalDbSnippets/ModuleInitializer.cs#L3-L14)</sup>
<!-- endsnippet -->


##### For EF:

<!-- snippet: ModuleInitializer -->
```cs
static class ModuleInitializer
{
    public static void Initialize()
    {
        SqlInstanceService.Register(
            name: "MySqlInstance",
            buildTemplate: TestDbBuilder.CreateTable);
    }
}
```
<sup>[snippet source](/src/LocalDbSnippets/ModuleInitializer.cs#L3-L14)</sup>
<!-- endsnippet -->

Or, alternatively, the module initializer can be injected with [PostSharp](https://doc.postsharp.net/module-initializer).


### Usage in a Test

Usage inside a test consists of two parts:


#### Build a SqlInstance


##### For SQL:

<!-- snippet: BuildLocalDbInstance -->
```cs
var database = await SqlInstanceService.Build();
```
<sup>[snippet source](/src/LocalDbSnippets/Tests.cs#L12-L14)</sup>
<!-- endsnippet -->


##### For EF:

<!-- snippet: EfBuildLocalDbInstance -->
```cs
var database = await SqlInstanceService<MyDbContext>.Build();
```
<sup>[snippet source](/src/EfLocalDbSnippets/Tests.cs#L12-L14)</sup>
<!-- endsnippet -->


#### BuildLocalDbSignature Signature

The signature is as follows:

<!-- snippet: BuildLocalDbSignature -->
```cs
/// <summary>
///   Build DB with a name based on the calling Method.
/// </summary>
/// <param name="testFile">The path to the test class. Used to make the db name unique per test type.</param>
/// <param name="databaseSuffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
/// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
```
<sup>[snippet source](/src/EfLocalDb/SqlInstance.cs#L156-L164)</sup>
<!-- endsnippet -->


#### Database Name

The database name is the derived as follows:


<!-- snippet: DeriveName -->
```cs
var dbName = $"{testClass}_{memberName}";
if (databaseSuffix != null)
{
    dbName = $"{dbName}_{databaseSuffix}";
}
```
<sup>[snippet source](/src/LocalDb/DbNamer.cs#L5-L13)</sup>
<!-- endsnippet -->

There is also an override that takes an explicit dbName:


##### For SQL:

<!-- snippet: WithDbName -->
```cs
var database = await SqlInstanceService.Build("TheTestWithDbName");
```
<sup>[snippet source](/src/LocalDbSnippets/Tests.cs#L34-L36)</sup>
<!-- endsnippet -->


##### For EF:

<!-- snippet: EfWithDbName -->
```cs
var database = await SqlInstanceService<MyDbContext>.Build("TheTestWithDbName");
```
<sup>[snippet source](/src/EfLocalDbSnippets/Tests.cs#L40-L42)</sup>
<!-- endsnippet -->


#### Building and using DbContexts/SQLConnection


##### For SQL:

<!-- snippet: BuildContext -->
```cs
using (var connection = await database.OpenConnection())
{
```
<sup>[snippet source](/src/LocalDbSnippets/Tests.cs#L16-L19)</sup>
<!-- endsnippet -->


##### For EF:

<!-- snippet: EfBuildContext -->
```cs
using (var dbContext = database.NewDbContext())
{
```
<sup>[snippet source](/src/EfLocalDbSnippets/Tests.cs#L16-L20)</sup>
<!-- endsnippet -->



#### Full Test

The above are combined in a full test:


##### For SQL:

<!-- snippet: Test -->
```cs
[Fact]
public async Task TheTest()
{
    var database = await SqlInstanceService.Build();

    using (var connection = await database.OpenConnection())
    {
        await TestDbBuilder.AddData(connection);
    }
    
    using (var connection = await database.OpenConnection())
    {
        Assert.Single(await TestDbBuilder.GetData(connection));
    }
}
```
<sup>[snippet source](/src/LocalDbSnippets/Tests.cs#L7-L29)</sup>
<!-- endsnippet -->


##### For EF:

<!-- snippet: EfTest -->
```cs
[Fact]
public async Task TheTest()
{
    var database = await SqlInstanceService<MyDbContext>.Build();

    using (var dbContext = database.NewDbContext())
    {
        var entity = new TheEntity
        {
            Property = "prop"
        };
        dbContext.Add(entity);
        dbContext.SaveChanges();
    }

    using (var dbContext = database.NewDbContext())
    {
        Assert.Single(dbContext.TestEntities);
    }
}
```
<sup>[snippet source](/src/EfLocalDbSnippets/Tests.cs#L7-L35)</sup>
<!-- endsnippet -->


### EF DefaultOptionsBuilder

When building a `DbContextOptionsBuilder` the default configuration is as follows:

<!-- snippet: DefaultOptionsBuilder.cs -->
```cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class DefaultOptionsBuilder
{
    public static DbContextOptionsBuilder<TDbContext> Build<TDbContext>()
        where TDbContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TDbContext>();
        builder.EnableSensitiveDataLogging();
        builder.EnableDetailedErrors();
        builder.ConfigureWarnings(warnings =>
        {
            warnings.Throw(CoreEventId.IncludeIgnoredWarning);
            warnings.Throw(RelationalEventId.QueryClientEvaluationWarning);
        });
        return builder;
    }
}
```
<sup>[snippet source](/src/EfLocalDb/DefaultOptionsBuilder.cs#L1-L19)</sup>
<!-- endsnippet -->


## Directory and Instance Name Resolution

The instance name is defined as:

<!-- snippet: GetInstanceName -->
```cs
if (scopeSuffix == null)
{
    return typeof(TDbContext).Name;
}

return $"{typeof(TDbContext).Name}_{scopeSuffix}";
```
<sup>[snippet source](/src/EfLocalDb/SqlInstance.cs#L134-L143)</sup>
<!-- endsnippet -->

That InstanceName is then used to derive the data directory. In order:

 * If `LocalDBData` environment variable exists then use `LocalDBData\InstanceName`.
 * If `AGENT_TEMPDIRECTORY` environment variable exists then use `AGENT_TEMPDIRECTORY\LocalDb\InstanceName`.
 * Use `%TempDir%\LocalDb\InstanceName`.

There is an explicit registration override that takes an instance name and a directory for that instance:


### For SQL:

<!-- snippet: RegisterExplcit -->
```cs
SqlInstanceService.Register(
    name: "theInstanceName",
    buildTemplate: TestDbBuilder.CreateTable,
    directory: @"C:\LocalDb\theInstance"
);
```
<sup>[snippet source](/src/LocalDbSnippets/Snippets.cs#L7-L15)</sup>
<!-- endsnippet -->


### For EF:

<!-- snippet: EfRegisterExplcit -->
```cs
SqlInstanceService<TheDbContext>.Register(
    buildTemplate: (connection, builder) =>
    {
        using (var dbContext = new TheDbContext(builder.Options))
        {
            dbContext.Database.EnsureCreated();
        }
    },
    constructInstance: builder => new TheDbContext(builder.Options),
    instanceName: "theInstanceName",
    directory: @"C:\LocalDb\theInstance"
);
```
<sup>[snippet source](/src/EfLocalDbSnippets/Snippets.cs#L7-L22)</sup>
<!-- endsnippet -->


## Debugging

To connect to a LocalDB instance using [SQL Server Management Studio ](https://docs.microsoft.com/en-us/sql/ssms/sql-server-management-studio-ssms?view=sql-server-2017) use a server name with the following convention `(LocalDb)\INSTANCENAME`.

So for a DbContext named `MyDbContext` the server name would be `(LocalDb)\MyDbContext`. Note that the name will be different if a `name` or `instanceSuffix` have been defined for SqlInstance.

The server name will be written to [Trace.WriteLine](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.trace.writeline) when a SqlInstance is constructed. It can be accessed programmatically from `SqlInstanceService.ServerName` or `SqlInstance.ServerName`.


## SqlLocalDb

The [SqlLocalDb Utility (SqlLocalDB.exe)](https://docs.microsoft.com/en-us/sql/tools/sqllocaldb-utility) utility is a simple command line tool to enable users and developers to create and manage an instance of LocalDB.

Useful commands:

 * `sqllocaldb info`: list all instances
 * `sqllocaldb create InstanceName`: create a new instance
 * `sqllocaldb start InstanceName`: start an instance
 * `sqllocaldb stop InstanceName`: stop an instance
 * `sqllocaldb delete InstanceName`: delete an instance (this does not delete the file system data for the instance)


## Icon

<a href="https://thenounproject.com/term/robot/960055/" target="_blank">Robot</a> designed by Creaticca Creative Agency from The Noun Project.
