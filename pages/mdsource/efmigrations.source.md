
# EF Migrations

[EF Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/) are supported.

snippet: Migrations

The above performs the following actions:


## Custom Migrations Operations

Optionally use [Custom Migrations Operations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/operations).

snippet: IMigrationsSqlGenerator


## Apply the migration

Perform a [Runtime apply of migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/#apply-migrations-at-runtime).

snippet: Migrate


## CheckForMigrations

Check if there are any pending migrations. This is an optional performance improvement. It allows the generated template to be re-used if there are no pending migrations.

snippet: CheckForMigrations