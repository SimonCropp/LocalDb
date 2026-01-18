namespace EfLocalDb;

/// <summary>
/// Represents a test database created from a template by <see cref="SqlInstance{TDbContext}"/> for Entity Framework 6 (Classic).
/// Provides access to the database connection, DbContext, and lifecycle management.
/// Each test should typically create its own <see cref="SqlDatabase{TDbContext}"/> instance via <see cref="SqlInstance{TDbContext}.Build(string, IEnumerable{object})"/>.
/// </summary>
/// <typeparam name="TDbContext">The Entity Framework 6 DbContext type.</typeparam>
/// <remarks>
/// The database is cloned from the template when created, providing test isolation.
/// Implements <see cref="IDisposable"/> for resource cleanup and <see cref="IServiceProvider"/> for dependency injection scenarios.
/// </remarks>
/// <example>
/// <code>
/// await using var database = await sqlInstance.Build();
///
/// // Use the provided DbContext
/// database.Context.Users.Add(new User { Name = "Test" });
/// await database.SaveChangesAsync();
///
/// // Or use implicit conversion
/// TDbContext context = database;
/// SqlConnection connection = database;
/// </code>
/// </example>
public partial class SqlDatabase<TDbContext> :
    IDisposable
    where TDbContext : DbContext
{
    ConstructInstance<TDbContext> constructInstance;
    Func<Task> delete;
    Func<Task>? takeOffline;
    IEnumerable<object>? data;

    internal SqlDatabase(
        SqlConnection connection,
        string name,
        ConstructInstance<TDbContext> constructInstance,
        Func<Task> delete,
        Func<Task>? takeOffline,
        IEnumerable<object>? data)
    {
        Name = name;
        this.constructInstance = constructInstance;
        this.delete = delete;
        this.takeOffline = takeOffline;
        this.data = data;
        ConnectionString = connection.ConnectionString;
        Connection = connection;
    }

    /// <summary>
    /// Gets the name of this database.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the open <see cref="SqlConnection"/> to this database.
    /// This connection is owned by the <see cref="SqlDatabase{TDbContext}"/> and will be disposed when the database is disposed.
    /// </summary>
    public SqlConnection Connection { get; }

    /// <summary>
    /// Gets the connection string for this database.
    /// Can be used to create additional connections via <see cref="OpenNewConnection"/>.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// Opens a new independent connection to this database.
    /// The caller is responsible for disposing the returned connection.
    /// </summary>
    /// <returns>A new open <see cref="SqlConnection"/> to this database.</returns>
    public async Task<SqlConnection> OpenNewConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    /// <summary>
    /// Implicit conversion to <typeparamref name="TDbContext"/> for convenience.
    /// Returns the <see cref="Context"/> property.
    /// </summary>
    public static implicit operator TDbContext(SqlDatabase<TDbContext> instance) => instance.Context;

    /// <summary>
    /// Implicit conversion to <see cref="SqlConnection"/> for convenience.
    /// Returns the <see cref="Connection"/> property.
    /// </summary>
    public static implicit operator SqlConnection(SqlDatabase<TDbContext> instance) => instance.Connection;

    internal Task Start()
    {
        Context = NewDbContext();
        if (data is not null)
        {
            return AddData(data);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the <typeparamref name="TDbContext"/> instance for this database.
    /// This context uses the <see cref="Connection"/> and is disposed when the database is disposed.
    /// </summary>
    public TDbContext Context { get; private set; } = null!;

    /// <summary>
    /// Calls <see cref="DbContext.SaveChanges()"/> on <see cref="Context"/>.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    public int SaveChanges() => Context.SaveChanges();

    /// <summary>
    /// Calls <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> on <see cref="Context"/>.
    /// </summary>
    /// <param name="cancel">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the number of state entries written.</returns>
    public Task<int> SaveChangesAsync(Cancel cancel = default) => Context.SaveChangesAsync(cancel);

    /// <summary>
    /// Creates a new <typeparamref name="TDbContext"/> instance using the existing <see cref="Connection"/>.
    /// The caller is responsible for disposing the returned context.
    /// The connection is shared, so do not dispose it independently.
    /// </summary>
    /// <returns>A new <typeparamref name="TDbContext"/> instance.</returns>
    public TDbContext NewDbContext() => constructInstance(Connection);

    /// <summary>
    /// Disposes the <see cref="Context"/> and <see cref="Connection"/>.
    /// If <c>dbAutoOffline</c> was enabled on the <see cref="SqlInstance{TDbContext}"/>, the database is also taken offline.
    /// </summary>
    public void Dispose()
    {
        // ReSharper disable once ConstantConditionalAccessQualifier
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        Context?.Dispose();
        Connection.Dispose();
        takeOffline?.Invoke().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Disposes the database resources and deletes the database from the LocalDB instance.
    /// Use this method when you want to explicitly remove the database files after the test.
    /// </summary>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    public Task Delete()
    {
        Dispose();
        return delete();
    }
}
