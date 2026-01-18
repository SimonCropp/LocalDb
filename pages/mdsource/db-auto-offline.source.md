# Database Auto-Offline

The `dbAutoOffline` parameter controls whether databases are automatically taken offline when disposed.

The default it to leave databases online after they go out scope. The reason for this behavior is to facilitate interrogating the database contents after a test has executed. 


## SqlInstance Usage

snippet: DbAutoOfflineUsage


## EF Core Usage

snippet: DbAutoOfflineUsageEfCore


## EF Classic Usage

snippet: DbAutoOfflineUsageEfClassic


## Behavior

When `dbAutoOffline: true`:

 * Database is taken offline when the `SqlDatabase` is disposed (not deleted)
 * `.mdf` and `.ldf` files remain in place on disk
 * Reduces LocalDB memory usage for large test suites
 * Database can be brought back online manually if needed


## Trade-offs

**Benefits:**

 * Lower memory usage during large test suites

**Drawbacks:**

 * If a test fails, the database must be manually brought online to inspect:

```sql
ALTER DATABASE [dbName] SET ONLINE;
```


## When to Use

Consider using `dbAutoOffline: true` when:

 * Running CI/CD pipelines where memory is constrained
 * Large test suites create many databases
 * Inspection of failed test databases is not frequently needed

Consider leaving the default (`dbAutoOffline: false`) when:

 * Test databases are regularly connected to via SSMS for debugging
 * Memory usage is not a concern
 * Databases need to remain accessible after tests


## CI Detection

To automatically enable `dbAutoOffline` in CI environments while keeping databases online during local development:

```cs
var isCI = Environment.GetEnvironmentVariable("CI") is not null;

var instance = new SqlInstance(
    "MyTests",
    BuildTemplate,
    dbAutoOffline: isCI);
```

