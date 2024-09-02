namespace EfLocalDb;

public partial class SqlInstance<TDbContext>
    where TDbContext : DbContext
{
    internal Wrapper Wrapper { get; } = null!;
    ConstructInstance<TDbContext> constructInstance = null!;
    static Storage DefaultStorage;
    Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder;

    static SqlInstance()
    {
        var type = typeof(TDbContext);
        var name = type.Name;
        if (type.IsNested)
        {
            name = $"{type.DeclaringType!.Name}_{name}";
        }
        DefaultStorage = new(name, DirectoryFinder.Find(name));
    }

    public IModel Model { get; } = null!;

    // ReSharper disable once UnusedMember.Global
    public string ServerName => Wrapper.ServerName;

    /// <summary>
    ///     Instantiate a <see cref="SqlInstance{TDbContext}" />.
    ///     Should usually be scoped as once instance per appdomain. So all tests use the same instance of
    ///     <see cref="SqlInstance{TDbContext}" />.
    /// </summary>
    /// <param name="constructInstance"></param>
    /// <param name="buildTemplate"></param>
    /// <param name="storage">Disk storage convention for where the mdb and the ldf files will be located.</param>
    /// <param name="timestamp"></param>
    /// <param name="templateSize">The size in MB for the template. Optional.</param>
    /// <param name="existingTemplate">Existing mdb and the ldf files to use when building the template. Optional.</param>
    /// <param name="callback">Option callback that is executed after the template database has been created.</param>
    /// <param name="sqlOptionsBuilder">Passed to <see cref="SqlServerDbContextOptionsExtensions.UseSqlServer(DbContextOptionsBuilder,string,Action{SqlServerDbContextOptionsBuilder})" />.</param>
    public SqlInstance(
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromContext<TDbContext>? buildTemplate = null,
        Storage? storage = null,
        DateTime? timestamp = null,
        ushort templateSize = 3,
        ExistingTemplate? existingTemplate = null,
        Callback<TDbContext>? callback = null,
        Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder = null) :
        this(
            constructInstance,
            BuildTemplateConverter.Convert(constructInstance, buildTemplate),
            storage,
            GetTimestamp(timestamp, buildTemplate),
            templateSize,
            existingTemplate,
            callback,
            sqlOptionsBuilder)
    {
    }

    /// <summary>
    ///     Instantiate a <see cref="SqlInstance{TDbContext}" />.
    ///     Should usually be scoped as one instance per appdomain. So all tests use the same instance of
    ///     <see cref="SqlInstance{TDbContext}" />.
    /// </summary>
    /// <param name="constructInstance"></param>
    /// <param name="buildTemplate"></param>
    /// <param name="storage">Disk storage convention for where the mdb and the ldf files will be located. Optional.</param>
    /// <param name="timestamp"></param>
    /// <param name="templateSize">The size in MB for the template. Optional.</param>
    /// <param name="existingTemplate">Existing mdb and the ldf files to use when building the template. Optional.</param>
    /// <param name="callback">Callback that is executed after the template database has been created. Optional.</param>
    /// <param name="sqlOptionsBuilder">Passed to <see cref="SqlServerDbContextOptionsExtensions.UseSqlServer(DbContextOptionsBuilder,string,Action{SqlServerDbContextOptionsBuilder})" />.</param>
    public SqlInstance(
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromConnection<TDbContext> buildTemplate,
        Storage? storage = null,
        DateTime? timestamp = null,
        ushort templateSize = 3,
        ExistingTemplate? existingTemplate = null,
        Callback<TDbContext>? callback = null,
        Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder = null)
    {
        if (!Guard.IsWindows)
        {
            return;
        }

        storage ??= DefaultStorage;
        var resultTimestamp = GetTimestamp(timestamp, buildTemplate);
        Model = BuildModel(constructInstance);
        InitEntityMapping();
        this.constructInstance = constructInstance;
        this.sqlOptionsBuilder = sqlOptionsBuilder;

        var storageValue = storage.Value;
        DirectoryCleaner.CleanInstance(storageValue.Directory);

        Task BuildTemplate(DbConnection connection)
        {
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseSqlServer(connection, sqlOptionsBuilder);
            return buildTemplate(connection, builder);
        }

        Func<DbConnection, Task>? wrapperCallback = null;
        if (callback is not null)
        {
            wrapperCallback = async connection =>
            {
                var builder = DefaultOptionsBuilder.Build<TDbContext>();
                builder.UseSqlServer(connection, sqlOptionsBuilder);
                await using var context = constructInstance(builder);
                await callback(connection, context);
            };
        }

        Wrapper = new(
            _ => new SqlConnection(_),
            storageValue.Name,
            storageValue.Directory,
            templateSize,
            existingTemplate,
            wrapperCallback);

        Wrapper.Start(resultTimestamp, BuildTemplate);
    }

    static DateTime GetTimestamp(DateTime? timestamp, Delegate? buildTemplate)
    {
        if (timestamp is not null)
        {
            return timestamp.Value;
        }

        if (buildTemplate is null)
        {
            return Timestamp.LastModified<TDbContext>();
        }

        return Timestamp.LastModified(buildTemplate);
    }

    static IModel BuildModel(ConstructInstance<TDbContext> constructInstance)
    {
        var builder = DefaultOptionsBuilder.Build<TDbContext>();
        builder.UseSqlServer("Fake");
        return constructInstance(builder).Model;
    }

    Task<string> BuildDatabase(string dbName) => Wrapper.CreateDatabaseFromTemplate(dbName);

    public string MasterConnectionString => Wrapper.MasterConnectionString;
}