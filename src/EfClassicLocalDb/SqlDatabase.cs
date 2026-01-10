namespace EfLocalDb;

public partial class SqlDatabase<TDbContext> :
    IDisposable
    where TDbContext : DbContext
{
    ConstructInstance<TDbContext> constructInstance;
    Func<Task> delete;
    IEnumerable<object>? data;

    internal SqlDatabase(
        string connectionString,
        string name,
        ConstructInstance<TDbContext> constructInstance,
        Func<Task> delete,
        IEnumerable<object>? data)
    {
        Name = name;
        this.constructInstance = constructInstance;
        this.delete = delete;
        this.data = data;
        ConnectionString = connectionString;
        Connection = new(connectionString);
    }

    internal SqlDatabase(
        string connectionString,
        string name,
        SqlConnection connection,
        ConstructInstance<TDbContext> constructInstance,
        Func<Task> delete,
        IEnumerable<object>? data)
    {
        Name = name;
        this.constructInstance = constructInstance;
        this.delete = delete;
        this.data = data;
        ConnectionString = connectionString;
        Connection = connection;
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

    public Task Start()
    {
        Context = NewDbContext();
        if (data is not null)
        {
            return AddData(data);
        }

        return Task.CompletedTask;
    }

    public TDbContext Context { get; private set; } = null!;

    /// <summary>
    ///     Calls <see cref="DbContext.SaveChanges()" /> on <see cref="Context" />.
    /// </summary>
    public int SaveChanges() => Context.SaveChanges();

    /// <summary>
    ///     Calls <see cref="DbContext.SaveChangesAsync(CancellationToken)" /> on <see cref="Context" />.
    /// </summary>
    public Task<int> SaveChangesAsync(Cancel cancel = default) => Context.SaveChangesAsync(cancel);

    public TDbContext NewDbContext() => constructInstance(Connection);

    public void Dispose()
    {
        // ReSharper disable once ConstantConditionalAccessQualifier
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        Context?.Dispose();
        Connection.Dispose();
    }

    public Task Delete()
    {
        Dispose();
        return delete();
    }
}
