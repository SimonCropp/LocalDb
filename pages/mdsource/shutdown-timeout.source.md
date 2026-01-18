# Shutdown Timeout

The `ShutdownTimeout` setting controls how long (in seconds) LocalDB waits before automatically shutting down after the last connection closes.


## Default Behavior

By default, LocalDB shuts down 30 seconds after the last connection closes. This is a balance between:

 * Keeping the instance running for quick successive test runs
 * Freeing up system resources when tests are complete

The minimum allowed value is 1 second. A value of 0 will throw an `ArgumentOutOfRangeException`.


## Configuration

The shutdown timeout can be configured in three ways (in order of precedence):


### Constructor Parameter

Pass `shutdownTimeout` when creating a `SqlInstance`:

```cs
// LocalDb
var instance = new SqlInstance(
    name: "MyInstance",
    buildTemplate: connection => ...,
    shutdownTimeout: 300);

// EF Core
var instance = new SqlInstance<MyDbContext>(
    constructInstance: builder => new MyDbContext(builder.Options),
    shutdownTimeout: 300);

// EF Classic
var instance = new SqlInstance<MyDbContext>(
    constructInstance: connection => new MyDbContext(connection),
    shutdownTimeout: 300);
```


### Environment Variable

Set the `LocalDBShutdownTimeout` environment variable to the desired number of seconds:

```bash
# Keep instance running for 5 minutes after last connection
set LocalDBShutdownTimeout=300
```


### Programmatic Configuration

Set `LocalDbSettings.ShutdownTimeout` before creating any `SqlInstance`:

```cs
// Keep instance running for 5 minutes after last connection
LocalDbSettings.ShutdownTimeout = 300;
```


## When to Adjust

**Increase the timeout** when:

 * Running tests interactively and want the instance to stay warm between runs
 * Debugging and need more time to inspect databases

**Decrease the timeout** when:

 * Running in CI where resources should be freed quickly
 * Memory is constrained and LocalDB instances should shut down promptly


## Technical Details

This setting configures the SQL Server `user instance timeout` advanced option via:

```sql
execute sp_configure 'user instance timeout', <seconds>;
```

The timeout is applied when the LocalDB instance is first started or rebuilt.
