# EntityFramework Usage

Interactions with LocalDB via [Entity Framework](https://docs.microsoft.com/en-us/ef/core/).


## EfLocalDb package [![NuGet Status](http://img.shields.io/nuget/v/EfLocalDb.svg)](https://www.nuget.org/packages/EfLocalDb/)

https://nuget.org/packages/EfLocalDb/


## Schema and data

The snippets use a DbContext of the following form:

snippet: TheDbContext.cs

snippet: TheEntity.cs


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


## Usage in a Test

Usage inside a test consists of two parts:


### Build a SqlInstance

snippet: EfBuildLocalDbInstance


### Build Signature

The signature is as follows:

snippet: EfBuildSignature


### Database Name

The database name is the derived as follows:

snippet: DeriveName

There is also an override that takes an explicit dbName:

snippet: EfWithDbName


### Using DbContexts

snippet: EfBuildContext


#### Full Test

The above are combined in a full test:

snippet: EfTest


### EF DefaultOptionsBuilder

When building a `DbContextOptionsBuilder` the default configuration is as follows:

snippet: DefaultOptionsBuilder.cs