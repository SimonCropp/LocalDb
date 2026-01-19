namespace LocalDb;

/// <summary>
/// Represents a test database created from a template by <see cref="SqlInstance"/>.
/// Provides access to the database connection and lifecycle management.
/// Each test should typically create its own <see cref="SqlDatabase"/> instance via <see cref="SqlInstance.Build(string)"/>.
/// </summary>
/// <remarks>
/// The database is cloned from the template when created, providing test isolation.
/// Implements <see cref="IDisposable"/> (and <see cref="IAsyncDisposable"/> on .NET 5+) for resource cleanup.
/// Also implements <see cref="IServiceProvider"/> for dependency injection scenarios.
/// </remarks>
/// <example>
/// <code>
/// await using var database = await sqlInstance.Build();
///
/// // Use the connection directly
/// await using var cmd = database.Connection.CreateCommand();
/// cmd.CommandText = "SELECT * FROM Users";
/// await using var reader = await cmd.ExecuteReaderAsync();
///
/// // Or use implicit conversion
/// SqlConnection connection = database;
/// </code>
/// </example>
public partial class SqlDatabase :
#if(NET5_0_OR_GREATER)
    IAsyncDisposable,
#endif
    IDisposable
{
    Func<Task> delete;
    Func<Task>? takeOffline;

    internal SqlDatabase(SqlConnection connection, string name, Func<Task> delete, Func<Task>? takeOffline = null)
    {
        this.delete = delete;
        this.takeOffline = takeOffline;
        ConnectionString = connection.ConnectionString;
        Name = name;
        Connection = connection;
    }

    /// <summary>
    /// Gets the connection string for this database.
    /// Can be used to create additional connections via <see cref="OpenNewConnection"/>.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// Gets the name of this database.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the open <see cref="SqlConnection"/> to this database.
    /// This connection is owned by the <see cref="SqlDatabase"/> and will be disposed when the database is disposed.
    /// </summary>
    public SqlConnection Connection { get; }

    /// <summary>
    /// Implicit conversion to <see cref="SqlConnection"/> for convenience.
    /// </summary>
    /// <param name="instance">The <see cref="SqlDatabase"/> instance.</param>
    /// <returns>The underlying <see cref="Connection"/>.</returns>
    public static implicit operator SqlConnection(SqlDatabase instance) => instance.Connection;

    /// <summary>
    /// Opens a new independent connection to this database.
    /// The caller is responsible for disposing the returned connection.
    /// </summary>
    /// <returns>A new open <see cref="SqlConnection"/> to this database.</returns>
    /// <example>
    /// <code>
    /// await using var newConnection = await database.OpenNewConnection();
    /// // Use newConnection independently of database.Connection
    /// </code>
    /// </example>
    public async Task<SqlConnection> OpenNewConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    /// <summary>
    /// Disposes the database connection.
    /// If <c>dbAutoOffline</c> was enabled on the <see cref="SqlInstance"/>, the database is also taken offline.
    /// </summary>
    public void Dispose()
    {
        Connection.Dispose();
        takeOffline?.Invoke().GetAwaiter().GetResult();
    }

#if(!NET48)
    /// <summary>
    /// Asynchronously disposes the database connection.
    /// If <c>dbAutoOffline</c> was enabled on the <see cref="SqlInstance"/>, the database is also taken offline.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
        if (takeOffline != null)
        {
            await takeOffline();
        }
    }
#endif

    /// <summary>
    /// Disposes the database connection and deletes the database from the LocalDB instance.
    /// Use this method when you want to explicitly remove the database files after the test.
    /// </summary>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    // ReSharper disable once ReplaceAsyncWithTaskReturn
    public async Task Delete()
    {
#if(NET48)
        Dispose();
#else
        await DisposeAsync();
#endif
        await delete();
    }
}
