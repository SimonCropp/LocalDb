# EntityFramework Core NUnit Usage

NUnit test base class that provides structured Arrange-Act-Assert testing with LocalDb integration and [Verify](https://github.com/VerifyTests/Verify) support.


## EfLocalDb.NUnit package [![NuGet Status](https://img.shields.io/nuget/v/EfLocalDb.NUnit.svg)](https://www.nuget.org/packages/EfLocalDb.NUnit/)

https://nuget.org/packages/EfLocalDb.NUnit/


## Overview

`LocalDbTestBase<T>` provides:

 * Automatic database creation per test method
 * Structured Arrange-Act-Assert phases with dedicated DbContext instances
 * SQL query recording via [Verify.EntityFramework](https://github.com/VerifyTests/Verify.EntityFramework)
 * Integration with [Verify](https://github.com/VerifyTests/Verify) for snapshot testing


## Schema

The snippets use a DbContext of the following form:

snippet: EfLocalDb.NUnit.Tests/Model/TheDbContext.cs

snippet: EfLocalDb.NUnit.Tests/Model/Company.cs


## Initialize

Initialize `LocalDbTestBase<T>` once using a `[ModuleInitializer]`:

snippet: EfLocalDb.NUnit.Tests/ModuleInitializer.cs


## Test Base Class

Inherit from `LocalDbTestBase<T>` to access test functionality:

```cs
[TestFixture]
public class MyTests : LocalDbTestBase<MyDbContext>
{
    [Test]
    public async Task MyTest()
    {
        // test code
    }
}
```


## Arrange-Act-Assert Pattern

The test base enforces a structured testing pattern with three phases:


### ArrangeData

Use `ArrangeData` to set up test preconditions. This DbContext has change tracking disabled for recording.

```cs
ArrangeData.Companies.Add(new Company { Id = Guid.NewGuid(), Name = "Test" });
await ArrangeData.SaveChangesAsync();
```


### ActData

Use `ActData` to perform the action being tested. Accessing `ActData` transitions the test from Arrange to Act phase and starts SQL recording.

```cs
var entity = await ActData.Companies.SingleAsync();
entity.Name = "Updated";
await ActData.SaveChangesAsync();
```


### AssertData

Use `AssertData` to verify the results. This DbContext uses `NoTracking` for clean reads. Accessing `AssertData` transitions to Assert phase and stops SQL recording.

```cs
var result = await AssertData.Companies.SingleAsync();
await Verify(result);
```


### Full Example

snippet: NUnitSimple


## Phase Enforcement

The test base enforces correct phase ordering:

 * `ArrangeData` can only be accessed during Arrange phase
 * `ActData` can only be accessed during Arrange or Act phase
 * `AssertData` can be accessed from any phase (transitions to Assert)
 * Accessing a phase out of order throws an exception


## Static Instance Access

For accessing the test base from helper methods, use `Instance`:

snippet: NUnitStaticInstance


## Verify Integration

The test base integrates with Verify for snapshot testing:


### VerifyEntity

Verify a single entity by primary key:

```cs
await VerifyEntity<Company>(company.Id);
```


### VerifyEntities

Verify a collection of entities:

```cs
await VerifyEntities(AssertData.Companies);
```


### Include Navigation Properties

```cs
await VerifyEntity<Company>(company.Id)
    .Include(_ => _.Employees)
    .ThenInclude(_ => _.Vehicles);
```


## Combination Testing

For parameterized tests with database reset between combinations:

snippet: NUnitCombinations


## Database Access

The underlying `SqlDatabase<T>` is available via the `Database` property:

```cs
var connectionString = Database.ConnectionString;
var name = Database.Name;
```


## Cleanup Behavior

 * On CI (detected via `BuildServerDetector`): Database is deleted after each test
 * Locally: Database is kept for debugging inspection
