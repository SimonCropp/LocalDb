# EntityFramework Migrations

[EntityFramework Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/) are supported via the `buildTemplate` parameter on `SqlInstance`. When using migrations, pass the `TemplateFromConnection` overload of `buildTemplate` so that the template database schema is created by applying migrations rather than `EnsureCreatedAsync`.

snippet: Migrations

The above performs the following actions:

 * Creates a `SqlInstance` using the connection-based `buildTemplate` delegate.
 * Optionally replaces `IMigrationsSqlGenerator` with a custom implementation.
 * Constructs a `DbContext` from the provided `DbContextOptionsBuilder`.
 * Applies all pending migrations via `Database.MigrateAsync()`.

The template database is built once and then cloned for each test, so migrations only run a single time regardless of how many tests execute.


## EnsureCreated vs Migrate

By default (when no `buildTemplate` is provided), `SqlInstance` uses `Database.EnsureCreatedAsync()` to create the schema. This is simpler but does not use migration history. If the project uses EF migrations, pass a `buildTemplate` that calls `Database.MigrateAsync()` instead. Do not mix both — `EnsureCreated` and `Migrate` are mutually exclusive in EF Core.


## Pending changes detection

When a `buildTemplate` delegate is provided via the context-based overload (`TemplateFromContext`), `SqlInstance` automatically calls `ThrowIfPendingChanges()` before building the template. This compares the current `DbContext` model against the latest migration snapshot and throws an `InvalidOperationException` listing the differences if any model changes have not been captured in a migration. This ensures the template database always matches the migration history and prevents silent schema drift during tests.


## Custom Migrations Operations

Optionally use [Custom Migrations Operations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/operations) by replacing `IMigrationsSqlGenerator` on the options builder before applying migrations.

snippet: IMigrationsSqlGenerator

This is useful for scenarios such as custom SQL generation, adding seed data via migration operations, or integrating with database-specific features not covered by the default SQL generator.


## Apply the migration

Perform a [Runtime apply of migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/#apply-migrations-at-runtime).

snippet: Migrate

This constructs a `DbContext` using the options builder (which is pre-configured to connect to the template database) and then applies all pending migrations. After this point the template database has the full schema and is ready to be cloned for individual tests.


## Testing a specific migration

To test a single migration in isolation, build the template up to the migration _before_ the one under test using `IMigrator.MigrateAsync("targetMigration")`. Each test then receives a clone at that point and can apply the next migration independently.

### Build the template to a known migration

snippet: MigrateToSpecificTarget

This uses `IMigrator` (resolved from the EF Core service provider) instead of `Database.MigrateAsync()` so a target migration name can be specified. The template is frozen at that point and cloned for every test.

### Apply and verify the next migration

snippet: TestSingleMigration

Each test gets its own database clone at the earlier migration state. It then applies the target migration and verifies the expected schema change. Because every test starts from the same cloned template, migrations under test are fully isolated from each other.