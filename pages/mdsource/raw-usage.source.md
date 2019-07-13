# Raw SQL Usage

Interactions with SqlLocalDB via a [SqlConnection](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlconnection).


## LocalDb package [![NuGet Status](http://img.shields.io/nuget/v/LocalDb.svg)](https://www.nuget.org/packages/LocalDb/)

https://nuget.org/packages/LocalDb/


## Schema and data

The snippets use the following helper class:

snippet: TestDbBuilder.cs


## Initialize SqlInstance

SqlInstance needs to be initialized once.

To ensure this happens only once there are several approaches that can be used:


### Static constructor

In the static constructor of a test.

If all tests that need to use the SqlInstance existing in the same test class, then the SqlInstance can be initialized in the static constructor of that test class.

snippet: StaticConstructor


### Static constructor in test base

If multiple tests need to use the SqlInstance, then the SqlInstance should be initialized in the static constructor of test base class.

snippet: TestBase


## Usage in a Test

Usage inside a test consists of two parts:


### Build a SqlDatabase

snippet: BuildDatabase

See: [Database Name Resolution](/pages/directory-and-name-resolution.md#database-name-resolution)


### Using SQLConnection

snippet: BuildContext


### Full Test

The above are combined in a full test:

snippet: SnippetTests.cs