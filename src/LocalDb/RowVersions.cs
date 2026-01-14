using System.Buffers.Binary;

#if EF
namespace EfLocalDb;
#else
namespace LocalDb;
#endif
public static class RowVersions
{    /// <summary>
    /// Retrieves the current row version (timestamp) for all rows in tables that have both an Id (UNIQUEIDENTIFIER) column and a RowVersion (ROWVERSION) column.
    /// </summary>
    /// <param name="connection">An open SQL Server connection.</param>
    /// <param name="cancel">The cancellation instruction.</param>
    /// <returns>
    /// A dictionary mapping entity IDs (GUID) to their row versions (ulong).
    /// The row version is a monotonically increasing value that changes every time the row is modified.
    /// Returns an empty dictionary if no tables match the criteria or if tables are empty.
    /// </returns>
    /// <remarks>
    /// This method dynamically discovers all base tables in the database that contain both:
    /// <list type="bullet">
    /// <item><description>An 'Id' column of type UNIQUEIDENTIFIER</description></item>
    /// <item><description>A 'RowVersion' column of type ROWVERSION</description></item>
    /// </list>
    /// It then queries all matching tables and returns the Id/RowVersion pairs for all rows.
    /// The row version is converted from SQL Server's 8-byte ROWVERSION to a ulong for easier comparison.
    /// </remarks>
    /// <example>
    /// <code>
    /// await using var connection = new SqlConnection(connectionString);
    /// await connection.OpenAsync();
    /// var rowVersions = await RowVersions.Read(connection);
    ///
    /// foreach (var (id, version) in rowVersions)
    /// {
    ///     Console.WriteLine($"Entity {id} has version {version}");
    /// }
    /// </code>
    /// </example>
    public static async Task<Dictionary<Guid, ulong>> Read(SqlConnection connection, Cancel cancel = default)
    {
        var sql = """
            DECLARE @sql nvarchar(max);

            SELECT @sql = STRING_AGG(
                CAST('SELECT Id, RowVersion FROM ' + QUOTENAME(t.TABLE_SCHEMA) + '.' + QUOTENAME(t.TABLE_NAME) AS NVARCHAR(MAX)),
                ' UNION ALL '
            )
            FROM INFORMATION_SCHEMA.TABLES t
            WHERE t.TABLE_TYPE = 'BASE TABLE'
              AND EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS c WHERE c.TABLE_NAME = t.TABLE_NAME AND c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.COLUMN_NAME = 'Id' AND c.DATA_TYPE = 'uniqueidentifier')
              AND EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS c WHERE c.TABLE_NAME = t.TABLE_NAME AND c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.COLUMN_NAME = 'RowVersion' AND c.DATA_TYPE = 'timestamp');

            EXEC sp_executesql @sql;
            """;

        var result = new Dictionary<Guid, ulong>();

#if NET5_0_OR_GREATER
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancel);
#else
        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync(cancel);
#endif

        while (await reader.ReadAsync(cancel))
        {
            var bytes = (byte[])reader[1];
            // SQL Server returns ROWVERSION in big-endian format
            result[reader.GetGuid(0)] = BinaryPrimitives.ReadUInt64BigEndian(bytes);
        }

        return result;
    }
}
