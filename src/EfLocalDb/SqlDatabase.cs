namespace EfLocalDb;

public partial class SqlDatabase<TDbContext> :
    IAsyncDisposable,
    IDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    ConstructInstance<TDbContext> constructInstance;
    Func<Task> delete;
    IEnumerable<object>? data;
    Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder;

    internal SqlDatabase(
        string connectionString,
        string name,
        ConstructInstance<TDbContext> constructInstance,
        Func<Task> delete,
        IEnumerable<object>? data,
        Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder)
    {
        Name = name;
        this.constructInstance = constructInstance;
        this.delete = delete;
        this.data = data;
        this.sqlOptionsBuilder = sqlOptionsBuilder;
        ConnectionString = connectionString;
        Connection = new(connectionString);
        findResult = GetType().GetMethod("FindResult", BindingFlags.Instance | BindingFlags.NonPublic)!;
        dataConnection = new(() =>
        {
            var connection = new DataSqlConnection(connectionString);
            connection.Open();
            return connection;
        });
    }

    public string Name { get; }
    public SqlConnection Connection { get; }
    Lazy<DataSqlConnection> dataConnection;
    MethodInfo findResult;
    public DataSqlConnection DataConnection => dataConnection.Value;
    public string ConnectionString { get; }

    public async Task<SqlConnection> OpenNewConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task<DataSqlConnection> OpenNewDataConnection()
    {
        var connection = new DataSqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public static implicit operator TDbContext(SqlDatabase<TDbContext> instance) => instance.Context;

    public static implicit operator SqlConnection(SqlDatabase<TDbContext> instance) => instance.Connection;

    public static implicit operator DbConnection(SqlDatabase<TDbContext> instance) => instance.Connection;

    public static implicit operator DataSqlConnection(SqlDatabase<TDbContext> instance) => instance.DataConnection;

    public async Task Start()
    {
        await Connection.OpenAsync();

        Context = NewDbContext();
        NoTrackingContext = NewDbContext(QueryTrackingBehavior.NoTracking);
        EntityTypes = Context.Model.GetEntityTypes().ToList();
        entityKeyMap = new();
        foreach (var entity in EntityTypes)
        {
            var key = entity.FindPrimaryKey();
            if (key is null)
            {
                continue;
            }

            var find = findResult.MakeGenericMethod(entity.ClrType);
            var keyTypes = key.Properties.Select(_ => _.ClrType).ToList();
            entityKeyMap.Add(new(entity, keyTypes, key, find));
        }
        if (data is not null)
        {
            await AddData(data);
        }
    }

    record EntityKeyMap(IEntityType Entity, List<Type> KeyTypes, IKey Key, MethodInfo Find);

    List<EntityKeyMap> entityKeyMap = null!;

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

    public IReadOnlyList<IEntityType> EntityTypes { get; private set; } = null!;

    public async ValueTask DisposeAsync()
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalse
        if (Context is not null)
        {
            await Context.DisposeAsync();
        }

        if (NoTrackingContext is not null)
        {
            await NoTrackingContext.DisposeAsync();
        }
        // ReSharper restore ConditionIsAlwaysTrueOrFalse

        await Connection.DisposeAsync();

        if (dataConnection.IsValueCreated)
        {
            await dataConnection.Value.DisposeAsync();
        }
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

    IEnumerable<object> ExpandEnumerable(IEnumerable<object> entities)
    {
        foreach (var entity in entities)
        {
            if (entity is IEnumerable enumerable)
            {
                var entityType = entity.GetType();
                if (EntityTypes.Any(_ => _.ClrType != entityType))
                {
                    foreach (var nested in enumerable)
                    {
                        yield return nested;
                    }

                    continue;
                }
            }

            yield return entity;
        }
    }
}