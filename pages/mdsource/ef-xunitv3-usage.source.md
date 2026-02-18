# EntityFramework Core xunit.v3 Usage

Combines [EfLocalDb](/pages/ef-usage.md), [xunit.v3](https://xunit.net/), [Verify.XunitV3](https://github.com/VerifyTests/Verify#verifyxunitv3), and [Verify.EntityFramework](https://github.com/VerifyTests/Verify.EntityFramework) into a test base class that provides an isolated database per test with [Arrange-Act-Assert](https://learn.microsoft.com/en-us/visualstudio/test/unit-test-basics#write-your-tests) phase enforcement.


## EfLocalDb.Xunit.V3 package [![NuGet Status](https://img.shields.io/nuget/v/EfLocalDb.Xunit.V3.svg)](https://www.nuget.org/packages/EfLocalDb.Xunit.V3/)

https://nuget.org/packages/EfLocalDb.Xunit.V3/


## Schema and data

The snippets use a DbContext of the following form:

snippet: EfLocalDb.MsTest.Tests/Model/TheDbContext.cs

snippet: EfLocalDb.MsTest.Tests/Model/Company.cs

snippet: EfLocalDb.MsTest.Tests/Model/Employee.cs


## Initialize

`LocalDbTestBase<T>.Initialize` needs to be called once. This is best done in a [ModuleInitializer](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/module-initializers):

snippet: EfLocalDb.Xunit.V3.Tests/ModuleInitializer.cs


## Usage in a Test

Inherit from `LocalDbTestBase<T>` and use the `ArrangeData`, `ActData`, and `AssertData` properties. These enforce phase ordering: accessing `ActData` transitions from Arrange to Act, and accessing `AssertData` transitions to Assert. Accessing a phase out of order throws an exception.

snippet: EfLocalDb.Xunit.V3.Tests/Tests.cs


## Static Instance

The current test instance can be accessed via `LocalDbTestBase<T>.Instance`. This is useful when test helpers need to access the database outside the test class:

snippet: StaticInstanceXunitV3


## Combinations

[Verify Combinations](https://github.com/VerifyTests/Verify#combinations) are supported. The database is reset for each combination:

snippet: CombinationsXunitV3


## VerifyEntity

Helpers for verifying entities by primary key, with optional Include/ThenInclude:

snippet: VerifyEntityXunitV3

snippet: VerifyEntityWithIncludeXunitV3

snippet: VerifyEntityWithThenIncludeXunitV3


## VerifyEntities

Verify a collection of entities from a `DbSet` or `IQueryable`:

snippet: VerifyEntities_DbSetXunitV3

snippet: VerifyEntity_QueryableXunitV3


## SharedDb

include: shared-db

snippet: SharedDbTestsXunitV3


## Parallel Execution

To run tests in parallel, configure parallelism at the assembly level:

snippet: EfLocalDb.Xunit.V3.Tests/TestConfig.cs
