# RowVersions

The `RowVersions` class provides utility methods for common SQL Server operations when working with LocalDb databases.

## Read

The `Read` method retrieves the current row version (timestamp) for all rows across all tables in a database that contain both an `Id` column (UNIQUEIDENTIFIER) and a `RowVersion` column (ROWVERSION).

### Usage

snippet: RowVersionsRead

### How It Works

The method:

1. Dynamically discovers all base tables in the database that have both:
   - An `Id` column of type UNIQUEIDENTIFIER
   - A `RowVersion` column of type ROWVERSION

2. Constructs a dynamic SQL query using `STRING_AGG` to union all matching tables

3. Returns a dictionary mapping entity IDs (Guid) to their row versions (ulong)

### Row Version Behavior

SQL Server's `ROWVERSION` (also known as `TIMESTAMP`) is a monotonically increasing value that:
- Automatically changes every time a row is modified
- Is unique within the database
- Can be used to detect if data has changed since it was last read

The method converts the 8-byte ROWVERSION value to a `ulong` for easier comparison in C# code.


### When to Use

Use cases:

- Track which entities have been modified
- Implement optimistic concurrency control
- Detect stale data in caching scenarios
- Synchronize data between systems


### Requirements

Tables must have:

- A column named `Id` of type `UNIQUEIDENTIFIER`
- A column named `RowVersion` of type `ROWVERSION`

Tables without either of these columns will be ignored by the query.
