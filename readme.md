<!--
This file was generate by MarkdownSnippets.
Source File: /readme.source.md
To change this file edit the source file and then re-run the generation using either the dotnet global tool (https://github.com/SimonCropp/MarkdownSnippets#markdownsnippetstool) or using the api (https://github.com/SimonCropp/MarkdownSnippets#running-as-a-unit-test).
-->
# EfLocalDb

Provides a wrapper around the [SQL Server Express LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) to simplify running tests that require Entity Framework](https://docs.microsoft.com/en-us/ef/core/).


## Why


### Why not [InMemory](https://docs.microsoft.com/en-us/ef/core/providers/in-memory/)

 * Difficult to debug the state
   When debugging a test, or looking at the final state, it is helpful to be able to interrogate the Database using tooling
 * InMemory is implemented with shared mutable state between instance. This results in strange behaviors when running tests in parallel, for example when [creating keys](https://github.com/aspnet/EntityFrameworkCore/issues/6872).
 * InMemory is not intended to be an alternative to SqlServer, and as such it does not support the full suite of SqlServer features. For example:
    * Does not support [Timestamp/row version](https://docs.microsoft.com/en-us/ef/core/modeling/concurrency#timestamprow-version).
    * [Does not validate constraints](https://github.com/aspnet/EntityFrameworkCore/issues/2166).


### Why not SQL Express or full SQL Server

 * Control over file location. LocalDB connections support AttachDbFileName property, which allows developers to specify a database file location. LocalDB will attach the specified database file and the connection will be made to it. This allows database files to be stored in a temporary location, and cleaned up, as required by tests.
 * No installed service is required.  Processes are started and stopped automatically when needed.
 * Automatic cleanup. A few minutes after the last connection to this process is closed the process shuts down.
 * Full control of instances using the [Command-Line Management Tool: SqlLocalDB.exe](https://docs.microsoft.com/en-us/sql/relational-databases/express-localdb-instance-apis/command-line-management-tool-sqllocaldb-exe?view=sql-server-2017).

References:

 * [Which Edition of SQL Server is Best for Development Work?](https://www.red-gate.com/simple-talk/sql/sql-development/edition-sql-server-best-development-work/#8)
 * [Introducing LocalDB, an improved SQL Express](https://blogs.msdn.microsoft.com/sqlexpress/2011/07/12/introducing-localdb-an-improved-sql-express/)


## The NuGet package [![NuGet Status](http://img.shields.io/nuget/v/EfLocalDb.svg?style=flat)](https://www.nuget.org/packages/EfLocalDb/)

https://nuget.org/packages/EfLocalDb/

    PM> Install-Package EfLocalDb


## Usage


### DbContext

Given a [DbContext](https://www.learnentityframeworkcore.com/dbcontext) as follows:

<!-- snippet: TheDbContext.cs -->
```cs
using Microsoft.EntityFrameworkCore;

public class TheDbContext :
    DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; }

    public TheDbContext(DbContextOptions options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<TestEntity>();
    }
}
```
<sup>[snippet source](/src/Snippets/TheDbContext.cs#L1-L17)</sup>
<!-- endsnippet -->


### Usage in a Test

Usage inside a test consists of two parts:


#### Build LocalDb Instance

<!-- snippet: BuildLocalDbInstance -->
```cs
var localDb = await LocalDb<TheDbContext>.Build(this);
```
<sup>[snippet source](/src/Snippets/Tests.cs#L12-L16)</sup>
<!-- endsnippet -->

The Build is as follows:

<!-- snippet: BuildLocalDbSignature -->
```cs
/// <summary>
///   Build DB with a name based on the calling Method.
/// </summary>
/// <param name="caller">Used to make the db name unique per type. Normally pass this.</param>
/// <param name="suffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
/// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
public static Task<LocalDb<TDbContext>> Build(
    object caller,
    string suffix = null,
    [CallerMemberName] string memberName = null)
{
```
<sup>[snippet source](/src/EfLocalDb/LocalDb.cs#L99-L112)</sup>
<!-- endsnippet -->

The database name is the derives as follows:

<!-- snippet: DeriveName -->
```cs
var type = caller.GetType();
var dbName = $"{type.Name}_{memberName}";
if (suffix != null)
{
    dbName = $"{dbName}_{suffix}";
}
```
<sup>[snippet source](/src/EfLocalDb/LocalDb.cs#L118-L127)</sup>
<!-- endsnippet -->

There is also an override that takes an explicit dbName:

<!-- snippet: WithDbName -->
```cs
var localDb = await LocalDb<TheDbContext>.Build("TheTestWithDbName");
```
<sup>[snippet source](/src/Snippets/Tests.cs#L42-L46)</sup>
<!-- endsnippet -->


#### Building and using DbContexts

<!-- snippet: BuildDbContext -->
```cs
using (var dbContext = localDb.NewDbContext())
{
```
<sup>[snippet source](/src/Snippets/Tests.cs#L18-L22)</sup>
<!-- endsnippet -->


#### Full Test

The above are combined in a full test:

<!-- snippet: Test -->
```cs
[Fact]
public async Task TheTest()
{

    var localDb = await LocalDb<TheDbContext>.Build(this);

    using (var dbContext = localDb.NewDbContext())
    {
        var entity = new TestEntity
        {
            Property = "prop"
        };
        dbContext.Add(entity);
        dbContext.SaveChanges();
    }

    using (var dbContext = localDb.NewDbContext())
    {
        Assert.Single(dbContext.TestEntities);
    }
}
```
<sup>[snippet source](/src/Snippets/Tests.cs#L7-L37)</sup>
<!-- endsnippet -->


### Initialize LocalDB

Once per implementation of DbContext, a LocalDB instance needs to be initialized.

To ensure this happens only once there are several approaches that can be used:


#### Static constructor

In the static constructor of a test.

If all tests that need to use the LocalDB instance existing in the same test class, then the LocalDB instance can be initialized in the static constructor of that test class.

<!-- snippet: StaticConstructor -->
```cs
public class Tests
{
    static Tests()
    {
        LocalDb<TheDbContext>.Register(
            (connection, optionsBuilder) =>
            {
                using (var dbContext = new TheDbContext(optionsBuilder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            builder => new TheDbContext(builder.Options));
    }

    [Fact]
    public async Task Test()
    {
        var localDb = await LocalDb<TheDbContext>.Build(this);
        using (var dbContext = localDb.NewDbContext())
        {
            var entity = new TestEntity
            {
                Property = "prop"
            };
            dbContext.Add(entity);
            dbContext.SaveChanges();
        }

        using (var dbContext = localDb.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }
    }
}
```
<sup>[snippet source](/src/Snippets/StaticConstructorTest.cs#L7-L45)</sup>
<!-- endsnippet -->


#### Static constructor in test base

If multiple tests need to use the LocalDB instance, then the LocalDB instance should be initialized in the static constructor of test base class.

<!-- snippet: TestBase -->
```cs
public class TestBase
{
    static TestBase()
    {
        LocalDb<TheDbContext>.Register(
            (connection, builder) =>
            {
                using (var dbContext = new TheDbContext(builder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            builder => new TheDbContext(builder.Options));
    }
}

public class Tests:
    TestBase
{
    [Fact]
    public async Task Test()
    {
        var localDb = await LocalDb<TheDbContext>.Build(this);
        using (var dbContext = localDb.NewDbContext())
        {
            var entity = new TestEntity
            {
                Property = "prop"
            };
            dbContext.Add(entity);
            dbContext.SaveChanges();
        }

        using (var dbContext = localDb.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }
    }
}
```
<sup>[snippet source](/src/Snippets/TestBaseUsage.cs#L7-L49)</sup>
<!-- endsnippet -->


#### Module initializer

An alternative to the above "test base" scenario is to use a module intializer. This can be achieved using the [Fody](https://github.com/Fody/Home) addin [ModuleInit](https://github.com/Fody/ModuleInit):

<!-- snippet: ModuleInitializer -->
```cs
static class ModuleInitializer
{
    public static void Initialize()
    {
        LocalDb<TheDbContext>.Register(
            (connection, optionsBuilder) =>
            {
                using (var dbContext = new TheDbContext(optionsBuilder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            builder => new TheDbContext(builder.Options));
    }
}
```
<sup>[snippet source](/src/Snippets/ModuleInitializer.cs#L5-L22)</sup>
<!-- endsnippet -->

Or, alternatively, the module initializer can be injected with [PostSharp](https://doc.postsharp.net/module-initializer).


### LocalDbTestBase

There is a helper class `LocalDbTestBase`:

<!-- snippet: LocalDbTestBase.cs -->
```cs
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EFLocalDb
{
    public abstract class LocalDbTestBase<T>
        where T : DbContext
    {
        /// <summary>
        ///   Build DB with a name based on the calling Method
        /// </summary>
        /// <param name="suffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
        /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
        public Task<LocalDb<T>> LocalDb(
            string suffix = null,
            [CallerMemberName] string memberName = null)
        {
            return LocalDb<T>.Build(this,suffix,memberName);
        }
    }
}
```
<sup>[snippet source](/src/EfLocalDb/LocalDbTestBase.cs#L1-L22)</sup>
```cs
using System.Threading.Tasks;
using EFLocalDb;
using Xunit;

namespace LocalDbTestBase
{
    #region LocalDbTestBase
    
    public class MyTestBase:
        LocalDbTestBase<TheDbContext>
    {
        static MyTestBase()
        {
            LocalDb<TheDbContext>.Register(
                (connection, optionsBuilder) =>
                {
                    using (var dbContext = new TheDbContext(optionsBuilder.Options))
                    {
                        dbContext.Database.EnsureCreated();
                    }
                },
                builder => new TheDbContext(builder.Options));
        }
    }

    public class Tests:
        MyTestBase
    {
        [Fact]
        public async Task Test()
        {
            var localDb = await LocalDb();
            using (var dbContext = localDb.NewDbContext())
            {
                var entity = new TestEntity
                {
                    Property = "prop"
                };
                dbContext.Add(entity);
                dbContext.SaveChanges();
            }

            using (var dbContext = localDb.NewDbContext())
            {
                Assert.Single(dbContext.TestEntities);
            }
        }
    }

    #endregion
}
```
<sup>[snippet source](/src/Snippets/LocalDbTestBase.cs#L1-L51)</sup>
<!-- endsnippet -->

`LocalDbTestBase` simplifies the construction of the LocalDb instance.

It can be used in combination with any of the above initialization methods. For example using a Static constructor in test base:

<!-- snippet: LocalDbTestBase -->
```md
public class MyTestBase:
    LocalDbTestBase<TheDbContext>
{
    static MyTestBase()
    {
        LocalDb<TheDbContext>.Register(
            (connection, optionsBuilder) =>
            {
                using (var dbContext = new TheDbContext(optionsBuilder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            builder => new TheDbContext(builder.Options));
    }
}

public class Tests:
    MyTestBase
{
    [Fact]
    public async Task Test()
    {
        var localDb = await LocalDb();
        using (var dbContext = localDb.NewDbContext())
        {
            var entity = new TestEntity
            {
                Property = "prop"
            };
            dbContext.Add(entity);
            dbContext.SaveChanges();
        }

        using (var dbContext = localDb.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }
    }
}
```
<sup>[snippet source](/readme.md#L345-L388)</sup>
```cs
public class MyTestBase:
    LocalDbTestBase<TheDbContext>
{
    static MyTestBase()
    {
        LocalDb<TheDbContext>.Register(
            (connection, optionsBuilder) =>
            {
                using (var dbContext = new TheDbContext(optionsBuilder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            builder => new TheDbContext(builder.Options));
    }
}

public class Tests:
    MyTestBase
{
    [Fact]
    public async Task Test()
    {
        var localDb = await LocalDb();
        using (var dbContext = localDb.NewDbContext())
        {
            var entity = new TestEntity
            {
                Property = "prop"
            };
            dbContext.Add(entity);
            dbContext.SaveChanges();
        }

        using (var dbContext = localDb.NewDbContext())
        {
            Assert.Single(dbContext.TestEntities);
        }
    }
}
```
<sup>[snippet source](/src/Snippets/LocalDbTestBase.cs#L7-L50)</sup>
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
<sup>[snippet source](/src/EfLocalDb/LocalDb.cs#L75-L84)</sup>
<!-- endsnippet -->

That InstanceName is then used to derive the data directory. In order:

 * If `LocalDBData` environment variable exists then use `AGENT_TEMPDIRECTORY\EfLocalDb\InstanceName`.
 * If `AGENT_TEMPDIRECTORY` environment variable exists then use `AGENT_TEMPDIRECTORY\EfLocalDb\InstanceName`.
 * Use `%TempDir%\EfLocalDb\InstanceName`

There is an explicit registration override that takes an instance name and a directory for that instance:

<!-- snippet: RegisterExplcit -->
```cs
LocalDb<TheDbContext>.Register(
    (connection, optionsBuilder) =>
    {
        using (var dbContext = new TheDbContext(optionsBuilder.Options))
        {
            dbContext.Database.EnsureCreated();
        }
    },
    builder => new TheDbContext(builder.Options),
    instanceName: "theInstanceName",
    directory: @"C:\EfLocalDb\theInstance"
);
```
<sup>[snippet source](/src/Snippets/Snippets.cs#L7-L22)</sup>
<!-- endsnippet -->


## Icon

<a href="https://thenounproject.com/term/robot/960055/" target="_blank">Robot</a> designed by Creaticca Creative Agency from The Noun Project.
