# EntityFramework Core xUnit v3 Usage

Combines [EfLocalDb](/pages/ef-usage.md), [xUnit v3](https://xunit.net/), [Verify.XunitV3](https://github.com/VerifyTests/Verify#verifyxunitv3), and [Verify.EntityFramework](https://github.com/VerifyTests/Verify.EntityFramework) into a test base class that provides an isolated database per test with [Arrange-Act-Assert](https://learn.microsoft.com/en-us/visualstudio/test/unit-test-basics#write-your-tests) phase enforcement.


## EfLocalDb.XunitV3 package [![NuGet Status](https://img.shields.io/nuget/v/EfLocalDb.XunitV3.svg)](https://www.nuget.org/packages/EfLocalDb.XunitV3/)

https://nuget.org/packages/EfLocalDb.XunitV3/


## Schema and data

The snippets use a DbContext of the following form:

snippet: EfLocalDb.XunitV3.Tests/Model/TheDbContext.cs

snippet: EfLocalDb.XunitV3.Tests/Model/Company.cs

snippet: EfLocalDb.XunitV3.Tests/Model/Employee.cs


## Initialize

`LocalDbTestBase<T>.Initialize` needs to be called once. This is best done in a [ModuleInitializer](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/module-initializers):

snippet: EfLocalDb.XunitV3.Tests/ModuleInitializer.cs


## Usage in a Test

Inherit from `LocalDbTestBase<T>` and use the `ArrangeData`, `ActData`, and `AssertData` properties. These enforce phase ordering: accessing `ActData` transitions from Arrange to Act, and accessing `AssertData` transitions to Assert. Accessing a phase out of order throws an exception.

snippet: EfLocalDb.XunitV3.Tests/Tests.cs


## Parallel Execution

To configure parallelism at the assembly level:

snippet: EfLocalDb.XunitV3.Tests/TestConfig.cs
