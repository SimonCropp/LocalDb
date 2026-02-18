# EntityFramework Core TUnit Usage

Combines [EfLocalDb](/pages/ef-usage.md), [TUnit](https://tunit.dev/), [Verify.TUnit](https://github.com/VerifyTests/Verify#verifytunit), and [Verify.EntityFramework](https://github.com/VerifyTests/Verify.EntityFramework) into a test base class that provides an isolated database per test with [Arrange-Act-Assert](https://learn.microsoft.com/en-us/visualstudio/test/unit-test-basics#write-your-tests) phase enforcement.


## EfLocalDb.TUnit package [![NuGet Status](https://img.shields.io/nuget/v/EfLocalDb.TUnit.svg)](https://www.nuget.org/packages/EfLocalDb.TUnit/)

https://nuget.org/packages/EfLocalDb.TUnit/


## Schema and data

The snippets use a DbContext of the following form:

snippet: EfLocalDb.TUnit.Tests/Model/TheDbContext.cs

snippet: EfLocalDb.TUnit.Tests/Model/Company.cs

snippet: EfLocalDb.TUnit.Tests/Model/Employee.cs


## Initialize

`LocalDbTestBase<T>.Initialize` needs to be called once. This is best done in a [ModuleInitializer](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/module-initializers):

snippet: EfLocalDb.TUnit.Tests/ModuleInitializer.cs


## Usage in a Test

Inherit from `LocalDbTestBase<T>` and use the `ArrangeData`, `ActData`, and `AssertData` properties. These enforce phase ordering: accessing `ActData` transitions from Arrange to Act, and accessing `AssertData` transitions to Assert. Accessing a phase out of order throws an exception.

snippet: EfLocalDb.TUnit.Tests/Tests.cs


## Static Instance

The current test instance can be accessed via `LocalDbTestBase<T>.Instance`. This is useful when test helpers need to access the database outside the test class:

snippet: StaticInstanceTUnit


## Combinations

[Verify Combinations](https://github.com/VerifyTests/Verify#combinations) are supported. The database is reset for each combination:

snippet: CombinationsTUnit


## VerifyEntity

Helpers for verifying entities by primary key, with optional Include/ThenInclude:

snippet: VerifyEntityTUnit

snippet: VerifyEntityWithIncludeTUnit

snippet: VerifyEntityWithThenIncludeTUnit


## VerifyEntities

Verify a collection of entities from a `DbSet` or `IQueryable`:

snippet: VerifyEntities_DbSetTUnit

snippet: VerifyEntity_QueryableTUnit


## SharedDb

include: shared-db

snippet: SharedDbTestsTUnit


## Parallel Execution

To run tests in parallel, configure parallelism at the assembly level:

snippet: EfLocalDb.TUnit.Tests/TestConfig.cs
