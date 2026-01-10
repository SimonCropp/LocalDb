namespace EfLocalDb;

public partial class SqlDatabase<TDbContext> :
    IAsyncDisposable,
    IDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    SqlInstance<TDbContext> instance;
    ConstructInstance<TDbContext> constructInstance;
    Func<Task> delete;
    IEnumerable<object>? data;
    Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder;

    internal SqlDatabase(
        SqlInstance<TDbContext> instance,
        string connectionString,
        string name,
        ConstructInstance<TDbContext> constructInstance,
        Func<Task> delete,
        IEnumerable<object>? data,
        Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder)
    {
        Name = name;
        this.instance = instance;
        this.constructInstance = constructInstance;
        this.delete = delete;
        this.data = data;
        this.sqlOptionsBuilder = sqlOptionsBuilder;
        ConnectionString = connectionString;
        Connection = new(connectionString);
    }

    public string Name { get; }
    public SqlConnection Connection { get; }
    public string ConnectionString { get; }

    public async Task<SqlConnection> OpenNewConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public static implicit operator TDbContext(SqlDatabase<TDbContext> instance) => instance.Context;

    public static implicit operator SqlConnection(SqlDatabase<TDbContext> instance) => instance.Connection;

    public async Task Start()
    {
        await Connection.OpenAsync();

        Context = NewDbContext();
        NoTrackingContext = NewDbContext(QueryTrackingBehavior.NoTracking);

        if (data is not null)
        {
            await AddData(data);
        }
    }

    public TDbContext Context { get; private set; } = null!;
    public TDbContext NoTrackingContext { get; private set; } = null!;

    TDbContext IDbContextFactory<TDbContext>.CreateDbContext() => NewConnectionOwnedDbContext();

    public TDbContext NewDbContext(QueryTrackingBehavior? tracking = null)
    {
        var builder = DefaultOptionsBuilder.Build<TDbContext>();
        builder.UseSqlServer(Connection, sqlOptionsBuilder);
        builder.ApplyQueryTracking(tracking);
        return constructInstance(builder);
    }

    public TDbContext NewConnectionOwnedDbContext(QueryTrackingBehavior? tracking = null)
    {
        var builder = DefaultOptionsBuilder.Build<TDbContext>();
        builder.UseSqlServer(Connection.ConnectionString, sqlOptionsBuilder);
        builder.ApplyQueryTracking(tracking);
        return constructInstance(builder);
    }

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

        await Connection.DisposeAsync();
    }

    public async Task Delete()
    {
        await DisposeAsync();
        await delete();
    }

    /// <summary>
    ///     Returns <see cref="DbContext.Set{TEntity}()" /> from <see cref="NoTrackingContext" />.
    /// </summary>
    public DbSet<T> Set<T>()
        where T : class => NoTrackingContext.Set<T>();

    IEnumerable<object> ExpandEnumerable(IEnumerable<object> entities) =>
        DbContextExtensions.ExpandEnumerable(entities, instance.EntityTypes);
}