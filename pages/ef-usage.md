<!--
GENERATED FILE - DO NOT EDIT
This file was generated by [MarkdownSnippets](https://github.com/SimonCropp/MarkdownSnippets).
Source File: /pages/mdsource/ef-usage.source.md
To change this file edit the source file and then run MarkdownSnippets.
-->

# EntityFramework Core Usage

Interactions with SqlLocalDB via [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/).


## EfLocalDb package [![NuGet Status](https://img.shields.io/nuget/v/EfLocalDb.svg)](https://www.nuget.org/packages/EfLocalDb/)

https://nuget.org/packages/EfLocalDb/


## Schema and data

The snippets use a DbContext of the following form:

<!-- snippet: EfLocalDb.Tests/Snippets/TheDbContext.cs -->
<a id='snippet-EfLocalDb.Tests/Snippets/TheDbContext.cs'></a>
```cs
public class TheDbContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<TheEntity> TestEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder model)
        => model.Entity<TheEntity>();
}
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/TheDbContext.cs#L1-L8' title='Snippet source file'>snippet source</a> | <a href='#snippet-EfLocalDb.Tests/Snippets/TheDbContext.cs' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: EfLocalDb.Tests/Snippets/TheEntity.cs -->
<a id='snippet-EfLocalDb.Tests/Snippets/TheEntity.cs'></a>
```cs
public class TheEntity
{
    public int Id { get; set; }
    public string? Property { get; set; }
}
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/TheEntity.cs#L1-L5' title='Snippet source file'>snippet source</a> | <a href='#snippet-EfLocalDb.Tests/Snippets/TheEntity.cs' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Initialize SqlInstance

SqlInstance needs to be initialized once.

To ensure this happens only once there are several approaches that can be used:


### Static constructor

In the static constructor of a test.

If all tests that need to use the SqlInstance existing in the same test class, then the SqlInstance can be initialized in the static constructor of that test class.

<!-- snippet: EfStaticConstructor -->
<a id='snippet-EfStaticConstructor'></a>
```cs
[TestFixture]
public class Tests
{
    static SqlInstance<TheDbContext> sqlInstance;

    static Tests() =>
        sqlInstance = new(
            builder => new(builder.Options));

    [Test]
    public async Task Test()
    {
        var entity = new TheEntity
        {
            Property = "prop"
        };
        await using var database = await sqlInstance.Build([entity]);
        AreEqual(1, database.Context.TestEntities.Count());
    }
}
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/StaticConstructor.cs#L11-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-EfStaticConstructor' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Static constructor in test base

If multiple tests need to use the SqlInstance, then the SqlInstance should be initialized in the static constructor of test base class.

<!-- snippet: EfTestBase -->
<a id='snippet-EfTestBase'></a>
```cs
public abstract class TestBase
{
    static SqlInstance<TheDbContext> sqlInstance;

    static TestBase() =>
        sqlInstance = new(
            constructInstance: builder => new(builder.Options));

    public static Task<SqlDatabase<TheDbContext>> LocalDb(
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "") =>
        sqlInstance.Build(testFile, databaseSuffix, memberName);
}

public class Tests :
    TestBase
{
    [Test]
    public async Task Test()
    {
        await using var database = await LocalDb();
        var entity = new TheEntity
        {
            Property = "prop"
        };
        await database.AddData(entity);

        AreEqual(1, database.Context.TestEntities.Count());
    }
}
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/EfTestBaseUsage.cs#L11-L45' title='Snippet source file'>snippet source</a> | <a href='#snippet-EfTestBase' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### SqlServerDbContextOptionsBuilder

Some SqlServer options are exposed by passing a `Action<SqlServerDbContextOptionsBuilder>` to the ` SqlServerDbContextOptionsExtensions.UseSqlServer`. In this project the `UseSqlServer` is handled internally, so the SqlServerDbContextOptionsBuilder functionality is achieved by passing a action to the SqlInstance.

<!-- snippet: sqlOptionsBuilder -->
<a id='snippet-sqlOptionsBuilder'></a>
```cs
var sqlInstance = new SqlInstance<MyDbContext>(
    constructInstance: builder => new(builder.Options),
    sqlOptionsBuilder: sqlBuilder => sqlBuilder.EnableRetryOnFailure(5));
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/SqlBuilder.cs#L9-L15' title='Snippet source file'>snippet source</a> | <a href='#snippet-sqlOptionsBuilder' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### Seeding data in the template

Data can be seeded into the template database for use across all tests:

<!-- snippet: EfBuildTemplate -->
<a id='snippet-EfBuildTemplate'></a>
```cs
public class BuildTemplate
{
    static SqlInstance<TheDbContext> sqlInstance;

    static BuildTemplate() =>
        sqlInstance = new(
            constructInstance: builder => new(builder.Options),
            buildTemplate: async context =>
            {
                await context.Database.EnsureCreatedAsync();
                var entity = new TheEntity
                {
                    Property = "prop"
                };
                context.Add(entity);
                await context.SaveChangesAsync();
            });

    [Test]
    public async Task BuildTemplateTest()
    {
        await using var database = await sqlInstance.Build();

        AreEqual(1, database.Context.TestEntities.Count());
    }
}
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/BuildTemplate.cs#L11-L40' title='Snippet source file'>snippet source</a> | <a href='#snippet-EfBuildTemplate' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Usage in a Test

Usage inside a test consists of two parts:


### Build a SqlDatabase

<!-- snippet: EfBuildDatabase -->
<a id='snippet-EfBuildDatabase'></a>
```cs
await using var database = await sqlInstance.Build();
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/EfSnippetTests.cs#L22-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-EfBuildDatabase' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

See: [Database Name Resolution](/pages/directory-and-name-resolution.md#database-name-resolution)


### Using DbContexts

<!-- snippet: EfBuildContext -->
<a id='snippet-EfBuildContext'></a>
```cs
await using (var data = database.NewDbContext())
{
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/EfSnippetTests.cs#L28-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-EfBuildContext' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


#### Full Test

The above are combined in a full test:

<!-- snippet: EfLocalDb.Tests/Snippets/EfSnippetTests.cs -->
<a id='snippet-EfLocalDb.Tests/Snippets/EfSnippetTests.cs'></a>
```cs
public class EfSnippetTests
{
    public class MyDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
    }

    static SqlInstance<MyDbContext> sqlInstance;

    static EfSnippetTests() =>
        sqlInstance = new(
            builder => new(builder.Options));


    [Test]
    public async Task TheTest()
    {

        await using var database = await sqlInstance.Build();



        await using (var data = database.NewDbContext())
        {


            var entity = new TheEntity
            {
                Property = "prop"
            };
            data.Add(entity);
            await data.SaveChangesAsync();
        }

        await using (var data = database.NewDbContext())
        {
            AreEqual(1, data.TestEntities.Count());
        }

    }

    [Test]
    public async Task TheTestWithDbName()
    {

        await using var database = await sqlInstance.Build("TheTestWithDbName");


        var entity = new TheEntity
        {
            Property = "prop"
        };
        await database.AddData(entity);

        AreEqual(1, database.Context.TestEntities.Count());
    }
}
```
<sup><a href='/src/EfLocalDb.Tests/Snippets/EfSnippetTests.cs#L1-L60' title='Snippet source file'>snippet source</a> | <a href='#snippet-EfLocalDb.Tests/Snippets/EfSnippetTests.cs' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


### EntityFramework DefaultOptionsBuilder

When building a `DbContextOptionsBuilder` the default configuration is as follows:

<!-- snippet: EfLocalDb/DefaultOptionsBuilder.cs -->
<a id='snippet-EfLocalDb/DefaultOptionsBuilder.cs'></a>
```cs
static class DefaultOptionsBuilder
{
    static LogCommandInterceptor interceptor = new();

    public static DbContextOptionsBuilder<TDbContext> Build<TDbContext>()
        where TDbContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TDbContext>();
        if (LocalDbLogging.SqlLoggingEnabled)
        {
            builder.AddInterceptors(interceptor);
        }
        builder.ReplaceService<IQueryProvider, QueryProvider>();
        builder.ReplaceService<IAsyncQueryProvider, QueryProvider>();
        builder.ReplaceService<IQueryCompilationContextFactory, QueryContextFactory>();
        builder.ReplaceService<ICompiledQueryCacheKeyGenerator, KeyGenerator>();

        builder.ConfigureWarnings(_ =>
        {
            _.Ignore(CoreEventId.SensitiveDataLoggingEnabledWarning);
            _.Default(WarningBehavior.Throw);
        });
        builder.EnableSensitiveDataLogging();
        builder.EnableDetailedErrors();
        return builder;
    }

    public static void ApplyQueryTracking<T>(this DbContextOptionsBuilder<T> builder, QueryTrackingBehavior? tracking)
        where T : DbContext
    {
        if (tracking.HasValue)
        {
            builder.UseQueryTrackingBehavior(tracking.Value);
        }
    }
}
```
<sup><a href='/src/EfLocalDb/DefaultOptionsBuilder.cs#L1-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-EfLocalDb/DefaultOptionsBuilder.cs' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
