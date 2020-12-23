# Classic EntityFramework Classic Usage

Interactions with SqlLocalDB via [Classic Entity Framework](https://docs.microsoft.com/en-us/ef/ef6/).


## EfClassicLocalDb package [![NuGet Status](https://img.shields.io/nuget/v/EfClassicLocalDb.svg)](https://www.nuget.org/packages/EfClassicLocalDb/)

https://nuget.org/packages/EfClassicLocalDb/


## Schema and data

The snippets use a DbContext of the following form:

snippet: EfClassicLocalDb.Tests/Snippets/TheDbContext.cs

snippet: EfClassicLocalDb.Tests/Snippets/TheEntity.cs


## Initialize SqlInstance

SqlInstance needs to be initialized once.

To ensure this happens only once there are several approaches that can be used:


### Static constructor

In the static constructor of a test.

If all tests that need to use the SqlInstance existing in the same test class, then the SqlInstance can be initialized in the static constructor of that test class.

snippet: EfClassicStaticConstructor


### Static constructor in test base

If multiple tests need to use the SqlInstance, then the SqlInstance should be initialized in the static constructor of test base class.

snippet: EfClassicTestBase


### Seeding data in the template

Data can be seeded into the template database for use across all tests:

snippet: EfClassicBuildTemplate


## Usage in a Test

Usage inside a test consists of two parts:


### Build a SqlDatabase

snippet: EfClassicBuildDatabase

See: [Database Name Resolution](/pages/directory-and-name-resolution.md#database-name-resolution)


### Using DbContexts

snippet: EfClassicBuildContext


#### Full Test

The above are combined in a full test:

snippet: EfClassicLocalDb.Tests/Snippets/EfSnippetTests.cs


## Using a pre-constructed template

It is possible to pass the path to a pre-existing template to SqlInstance. This is useful if the database contains stored procedures, or requires a large amount of test data.

snippet: EfClassicLocalDb.Tests/Snippets/SuppliedTemplate.cs