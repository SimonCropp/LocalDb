# Database Auto-Offline

The `dbAutoOffline` parameter controls whether databases are automatically taken offline when disposed.

By default (`dbAutoOffline: null`), auto-offline is enabled when a CI environment is detected, and disabled otherwise. This means:

 * In CI environments: databases are taken offline automatically to reduce memory usage
 * In local development: databases remain online for inspection via SSMS

To override this behavior, explicitly set `dbAutoOffline: true` or `dbAutoOffline: false`.


## Detected CI Systems

The following CI systems are automatically detected:

 * **AppVeyor, Travis CI, CircleCI, GitLab CI** - `CI` = `true` or `1`
 * **Azure DevOps** - `TF_BUILD` = `True`
 * **GitHub Actions** - `GITHUB_ACTIONS` = `true`
 * **TeamCity** - `TEAMCITY_VERSION` is set
 * **Jenkins** - `JENKINS_URL` is set


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

The default behavior (CI auto-detection) is recommended for most scenarios. Override only when needed:

Use `dbAutoOffline: true` when:

 * Running locally but want to simulate CI behavior
 * Memory is constrained even in local development

Use `dbAutoOffline: false` when:

 * Running in CI but need databases to remain online for debugging
 * Post-test database inspection is required in all environments

