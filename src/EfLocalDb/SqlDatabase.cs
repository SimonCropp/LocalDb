namespace EfLocalDb;

/// <summary>
/// Represents a test database created from a template by <see cref="SqlInstance{TDbContext}"/> for Entity Framework Core.
/// Provides access to the database connection, DbContext instances, and lifecycle management.
/// Each test should typically create its own <see cref="SqlDatabase{TDbContext}"/> instance via <see cref="SqlInstance{TDbContext}.Build(string, IEnumerable{object})"/>.
/// </summary>
/// <typeparam name="TDbContext">The Entity Framework Core DbContext type.</typeparam>
/// <remarks>
/// <para>
/// The database is cloned from the template when created, providing test isolation.
/// </para>
/// <para>
/// Implements <see cref="IAsyncDisposable"/> for resource cleanup, <see cref="IDbContextFactory{TDbContext}"/> for creating
/// additional DbContext instances, and <see cref="IServiceProvider"/> for dependency injection scenarios.
/// </para>
/// <para>
/// Provides two pre-configured DbContext instances: <see cref="Context"/> (with tracking) for modifications,
/// and <see cref="NoTrackingContext"/> (without tracking) for read-only queries via <see cref="Set{T}"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// await using var database = await sqlInstance.Build();
///
/// // Use the provided DbContext for modifications
/// database.Context.Users.Add(new User { Name = "Test" });
/// await database.SaveChangesAsync();
///
/// // Use Set&lt;T&gt;() for read-only queries (uses NoTrackingContext)
/// var users = await database.Set&lt;User&gt;().ToListAsync();
///
/// // Or use implicit conversion
/// TDbContext context = database;
/// SqlConnection connection = database;
/// </code>
/// </example>
public partial class SqlDatabase<TDbContext> :
    IAsyncDisposable,
    IDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    SqlInstance<TDbContext> instance;
    ConstructInstance<TDbContext> constructInstance;
    Func<Task> delete;
    Func<Task>? takeOffline;
    IEnumerable<object>? data;
    Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder;

    internal SqlDatabase(
        SqlInstance<TDbContext> instance,
        SqlConnection connection,
        string name,
        ConstructInstance<TDbContext> constructInstance,
        Func<Task> delete,
        Func<Task>? takeOffline,
        IEnumerable<object>? data,
        Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder,
        SqlTransaction? transaction = null)
    {
        Name = name;
        this.instance = instance;
        this.constructInstance = constructInstance;
        this.delete = delete;
        this.takeOffline = takeOffline;
        this.data = data;
        this.sqlOptionsBuilder = sqlOptionsBuilder;
        ConnectionString = connection.ConnectionString;
        Connection = connection;
        Transaction = transaction;
    }

    /// <summary>
    /// Gets the <see cref="SqlTransaction"/> associated with this database, if any.
    /// When set, the transaction is rolled back and disposed when the database is disposed.
    /// </summary>
    public SqlTransaction? Transaction { get; }

    /// <summary>
    /// Gets the name of this database.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the open <see cref="SqlConnection"/> to this database.
    /// This connection is shared by <see cref="Context"/> and <see cref="NoTrackingContext"/>,
    /// and will be disposed when the database is disposed.
    /// </summary>
    public SqlConnection Connection { get; }

    /// <summary>
    /// Gets the connection string for this database.
    /// Can be used to create additional connections via <see cref="OpenNewConnection"/> or <see cref="NewConnectionOwnedDbContext"/>.
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
        NoTrackingContext = NewDbContext(QueryTrackingBehavior.NoTracking);

        if (Transaction is not null)
        {
            Context.Database.UseTransaction(Transaction);
            NoTrackingContext.Database.UseTransaction(Transaction);
        }

        if (data is not null)
        {
            return AddData(data);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the <typeparamref name="TDbContext"/> instance for this database with change tracking enabled.
    /// Use this context for adding, updating, or deleting entities.
    /// This context uses the shared <see cref="Connection"/> and is disposed when the database is disposed.
    /// </summary>
    public TDbContext Context { get; private set; } = null!;

    /// <summary>
    /// Gets a <typeparamref name="TDbContext"/> instance with <see cref="QueryTrackingBehavior.NoTracking"/>.
    /// Use this context (or <see cref="Set{T}"/>) for read-only queries to avoid tracking overhead.
    /// This context uses the shared <see cref="Connection"/> and is disposed when the database is disposed.
    /// </summary>
    public TDbContext NoTrackingContext { get; private set; } = null!;

    /// <summary>
    /// Creates a new <typeparamref name="TDbContext"/> instance that owns its own connection.
    /// Implements <see cref="IDbContextFactory{TDbContext}.CreateDbContext"/>.
    /// The returned context manages its own connection lifecycle.
    /// </summary>
    /// <returns>A new <typeparamref name="TDbContext"/> instance with its own connection.</returns>
    TDbContext IDbContextFactory<TDbContext>.CreateDbContext() => NewConnectionOwnedDbContext();

    /// <summary>
    /// Creates a new <typeparamref name="TDbContext"/> instance using the shared <see cref="Connection"/>.
    /// The caller is responsible for disposing the returned context.
    /// The connection is shared, so do not dispose it independently.
    /// </summary>
    /// <param name="tracking">Optional query tracking behavior. If null, uses the DbContext default.</param>
    /// <returns>A new <typeparamref name="TDbContext"/> instance.</returns>
    public TDbContext NewDbContext(QueryTrackingBehavior? tracking = null)
    {
        var builder = DefaultOptionsBuilder.Build<TDbContext>();
        builder.UseSqlServer(Connection, sqlOptionsBuilder);
        builder.ApplyQueryTracking(tracking);
        return constructInstance(builder);
    }

    /// <summary>
    /// Creates a new <typeparamref name="TDbContext"/> instance with its own connection.
    /// The returned context owns and manages its connection lifecycle independently.
    /// Use this when you need a context that outlives the <see cref="SqlDatabase{TDbContext}"/>.
    /// </summary>
    /// <param name="tracking">Optional query tracking behavior. If null, uses the DbContext default.</param>
    /// <returns>A new <typeparamref name="TDbContext"/> instance with its own connection.</returns>
    public TDbContext NewConnectionOwnedDbContext(QueryTrackingBehavior? tracking = null)
    {
        var builder = DefaultOptionsBuilder.Build<TDbContext>();
        builder.UseSqlServer(Connection.ConnectionString, sqlOptionsBuilder);
        builder.ApplyQueryTracking(tracking);
        return constructInstance(builder);
    }

    /// <summary>
    /// Asynchronously disposes <see cref="Context"/>, <see cref="NoTrackingContext"/>, and <see cref="Connection"/>.
    /// If <c>dbAutoOffline</c> was enabled on the <see cref="SqlInstance{TDbContext}"/>, the database is also taken offline.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalse
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Context is not null)
        {
            await Context.DisposeAsync();
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (NoTrackingContext is not null)
        {
            await NoTrackingContext.DisposeAsync();
        }
        // ReSharper restore ConditionIsAlwaysTrueOrFalse

        if (Transaction != null)
        {
            await Transaction.RollbackAsync();
            await Transaction.DisposeAsync();
        }

        await Connection.DisposeAsync();
        if (takeOffline != null)
        {
            await takeOffline();
        }
    }

    /// <summary>
    /// Disposes the database resources and deletes the database from the LocalDB instance.
    /// Use this method when you want to explicitly remove the database files after the test.
    /// </summary>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    public async Task Delete()
    {
        await DisposeAsync();
        await delete();
    }

    /// <summary>
    /// Returns <see cref="DbContext.Set{TEntity}()"/> from <see cref="NoTrackingContext"/>.
    /// Use for read-only queries without change tracking overhead.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A <see cref="DbSet{T}"/> for the specified entity type with no tracking.</returns>
    public DbSet<T> Set<T>()
        where T : class => NoTrackingContext.Set<T>();

    IEnumerable<object> ExpandEnumerable(IEnumerable<object> entities) =>
        DbContextExtensions.ExpandEnumerable(entities, instance.EntityTypes);
}
