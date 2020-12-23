# EntityFramework Core Usage

Interactions with SqlLocalDB via [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/).


## EfLocalDb package [![NuGet Status](https://img.shields.io/nuget/v/EfLocalDb.svg)](https://www.nuget.org/packages/EfLocalDb/)

https://nuget.org/packages/EfLocalDb/


## Schema and data

The snippets use a DbContext of the following form:

snippet: EfLocalDb.Tests/Snippets/TheDbContext.cs

snippet: EfLocalDb.Tests/Snippets/TheEntity.cs


## Initialize SqlInstance

SqlInstance needs to be initialized once.

To ensure this happens only once there are several approaches that can be used:


### Static constructor

In the static constructor of a test.

If all tests that need to use the SqlInstance existing in the same test class, then the SqlInstance can be initialized in the static constructor of that test class.

snippet: EfStaticConstructor


### Static constructor in test base

If multiple tests need to use the SqlInstance, then the SqlInstance should be initialized in the static constructor of test base class.

snippet: EfTestBase


### SqlServerDbContextOptionsBuilder

Some SqlServer options are exposed by passing a `Action<SqlServerDbContextOptionsBuilder>` to the ` SqlServerDbContextOptionsExtensions.UseSqlServer`. In this project the `UseSqlServer` is handled internally, so the SqlServerDbContextOptionsBuilder functionality is achieved by passing a action to the SqlInstance.

snippet: sqlOptionsBuilder


### Seeding data in the template

Data can be seeded into the template database for use across all tests:

snippet: EfBuildTemplate


## Usage in a Test

Usage inside a test consists of two parts:


### Build a SqlDatabase

snippet: EfBuildDatabase

See: [Database Name Resolution](/pages/directory-and-name-resolution.md#database-name-resolution)


### Using DbContexts

snippet: EfBuildContext


#### Full Test

The above are combined in a full test:

snippet: EfLocalDb.Tests/Snippets/EfSnippetTests.cs


### EntityFramework DefaultOptionsBuilder

When building a `DbContextOptionsBuilder` the default configuration is as follows:

snippet: EfLocalDb/DefaultOptionsBuilder.cs