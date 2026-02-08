# EntityFramework Core NUnit Usage

Combines [EfLocalDb](/pages/ef-usage.md), [NUnit](https://nunit.org/), [Verify.NUnit](https://github.com/VerifyTests/Verify#verifynunit), and [Verify.EntityFramework](https://github.com/VerifyTests/Verify.EntityFramework) into a test base class that provides an isolated database per test with [Arrange-Act-Assert](https://learn.microsoft.com/en-us/visualstudio/test/unit-test-basics#write-your-tests) phase enforcement.


## EfLocalDb.NUnit package [![NuGet Status](https://img.shields.io/nuget/v/EfLocalDb.NUnit.svg)](https://www.nuget.org/packages/EfLocalDb.NUnit/)

https://nuget.org/packages/EfLocalDb.NUnit/


## Schema and data

The snippets use a DbContext of the following form:

snippet: EfLocalDb.NUnit.Tests/Model/TheDbContext.cs

snippet: EfLocalDb.NUnit.Tests/Model/Company.cs

snippet: EfLocalDb.NUnit.Tests/Model/Employee.cs


## Initialize

`LocalDbTestBase<T>.Initialize` needs to be called once. This is best done in a [ModuleInitializer](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/module-initializers):

snippet: EfLocalDb.NUnit.Tests/ModuleInitializer.cs


## Usage in a Test

Inherit from `LocalDbTestBase<T>` and use the `ArrangeData`, `ActData`, and `AssertData` properties. These enforce phase ordering: accessing `ActData` transitions from Arrange to Act, and accessing `AssertData` transitions to Assert. Accessing a phase out of order throws an exception.

snippet: EfLocalDb.NUnit.Tests/Tests.cs


## Static Instance

The current test instance can be accessed via `LocalDbTestBase<T>.Instance`. This is useful when test helpers need to access the database outside the test class:

snippet: StaticInstance


## Combinations

[Verify Combinations](https://github.com/VerifyTests/Verify#combinations) are supported. The database is reset for each combination:

snippet: Combinations


## VerifyEntity

Helpers for verifying entities by primary key, with optional Include/ThenInclude:

snippet: VerifyEntity

snippet: VerifyEntityWithInclude

snippet: VerifyEntityWithThenInclude


## VerifyEntities

Verify a collection of entities from a `DbSet` or `IQueryable`:

snippet: VerifyEntities_DbSet

snippet: VerifyEntity_Queryable


## Parallel Execution

To run tests in parallel, configure parallelism at the assembly level:

snippet: EfLocalDb.NUnit.Tests/TestConfig.cs
