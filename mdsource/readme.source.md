# EfLocalDb

Provides a wrapper around [LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) to simplify running tests that require [Entity Framework](https://docs.microsoft.com/en-us/ef/core/).


## Why


### Why not [InMemory](https://docs.microsoft.com/en-us/ef/core/providers/in-memory/)

 * Difficult to debug the state
   When debugging a test, or looking at the resultant state, it is helpful to be able to interrogate the Database using tooling
 * InMemory is implemented with shared mutable state between instance. This results in strange behaviors when running tests in parallel, for example when [creating keys](https://github.com/aspnet/EntityFrameworkCore/issues/6872).
 * InMemory is not intended to be an alternative to SqlServer, and as such it does not support the full suite of SqlServer features. For example:
    * Does not support [Timestamp/row version](https://docs.microsoft.com/en-us/ef/core/modeling/concurrency#timestamprow-version).
    * [Does not validate constraints](https://github.com/aspnet/EntityFrameworkCore/issues/2166).


### Why not SQL Express or full SQL Server

 * Control over file location. LocalDB connections support AttachDbFileName property, which allows developers to specify a database file location. LocalDB will attach the specified database file and the connection will be made to it. This allows database files to be stored in a temporary location, and cleaned up, as required by tests.
 * No installed service is required.  Processes are started and stopped automatically when needed.
 * Automatic cleanup. A few minutes after the last connection to this process is closed the process shuts down.
 * Full control of instances using the [Command-Line Management Tool: SqlLocalDB.exe](https://docs.microsoft.com/en-us/sql/relational-databases/express-localdb-instance-apis/command-line-management-tool-sqllocaldb-exe?view=sql-server-2017).

References:

 * [Which Edition of SQL Server is Best for Development Work?](https://www.red-gate.com/simple-talk/sql/sql-development/edition-sql-server-best-development-work/#8)
 * [Introducing LocalDB, an improved SQL Express](https://blogs.msdn.microsoft.com/sqlexpress/2011/07/12/introducing-localdb-an-improved-sql-express/)


## The NuGet package [![NuGet Status](http://img.shields.io/nuget/v/EfLocalDb.svg?style=flat)](https://www.nuget.org/packages/EfLocalDb/)

https://nuget.org/packages/EfLocalDb/

    PM> Install-Package EfLocalDb


## Usage


### DbContext

Given a [DbContext](https://www.learnentityframeworkcore.com/dbcontext) as follows:

snippet: TheDbContext.cs


### Usage in a Test

Usage inside a test consists of two parts:


#### Build LocalDb Instance

snippet: BuildLocalDbInstance

The signature is as follows:

snippet: BuildLocalDbSignature

The database name is the derived as follows:

snippet: DeriveName

There is also an override that takes an explicit dbName:

snippet: WithDbName


#### Building and using DbContexts

snippet: BuildDbContext


#### Full Test

The above are combined in a full test:

snippet: Test


### Initialize LocalDB

Once per implementation of DbContext, a LocalDB instance needs to be initialized.

To ensure this happens only once there are several approaches that can be used:


#### Static constructor

In the static constructor of a test.

If all tests that need to use the LocalDB instance existing in the same test class, then the LocalDB instance can be initialized in the static constructor of that test class.

snippet: StaticConstructor


#### Static constructor in test base

If multiple tests need to use the LocalDB instance, then the LocalDB instance should be initialized in the static constructor of test base class.

snippet: TestBase


#### Module initializer

An alternative to the above "test base" scenario is to use a module intializer. This can be achieved using the [Fody](https://github.com/Fody/Home) addin [ModuleInit](https://github.com/Fody/ModuleInit):

snippet: ModuleInitializer

Or, alternatively, the module initializer can be injected with [PostSharp](https://doc.postsharp.net/module-initializer).


### LocalDbTestBase

There is a helper class `LocalDbTestBase`:

snippet: LocalDbTestBase.cs

`LocalDbTestBase` simplifies the construction of the LocalDb instance.

It can be used in combination with any of the above initialization methods. For example using a Static constructor in test base:

snippet: LocalDbTestBaseUsage


## Directory and Instance Name Resolution

The instance name is defined as: 

snippet: GetInstanceName

That InstanceName is then used to derive the data directory. In order:

 * If `LocalDBData` environment variable exists then use `LocalDBData\InstanceName`.
 * If `AGENT_TEMPDIRECTORY` environment variable exists then use `AGENT_TEMPDIRECTORY\EfLocalDb\InstanceName`.
 * Use `%TempDir%\EfLocalDb\InstanceName`

There is an explicit registration override that takes an instance name and a directory for that instance:

snippet: RegisterExplcit


## Icon

<a href="https://thenounproject.com/term/robot/960055/" target="_blank">Robot</a> designed by Creaticca Creative Agency from The Noun Project.