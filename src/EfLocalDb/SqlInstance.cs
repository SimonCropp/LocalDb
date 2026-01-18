namespace EfLocalDb;

/// <summary>
/// Manages the lifecycle of a SQL Server LocalDB instance for Entity Framework Core testing.
/// Provides template-based database creation for efficient test isolation with DbContext integration.
/// </summary>
/// <typeparam name="TDbContext">The Entity Framework Core DbContext type used for database operations.</typeparam>
public partial class SqlInstance<TDbContext> :
    IDisposable
    where TDbContext : DbContext
{
    internal Wrapper Wrapper { get; } = null!;
    ConstructInstance<TDbContext> constructInstance = null!;
    static Storage defaultStorage;
    Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder;
    bool dbAutoOffline;

    static SqlInstance()
    {
        var type = typeof(TDbContext);
        var name = type.Name;
        if (type.IsNested)
        {
            name = $"{type.DeclaringType!.Name}_{name}";
        }

        defaultStorage = new(name, DirectoryFinder.Find(name));
    }

    public IModel Model { get; } = null!;

    // ReSharper disable once UnusedMember.Global
    public string ServerName => Wrapper.ServerName;

    /// <summary>
    /// Instantiate a <see cref="SqlInstance{TDbContext}"/> using a context-based template builder.
    /// Should usually be scoped as one instance per AppDomain, so all tests use the same instance.
    /// </summary>
    /// <param name="constructInstance">
    /// A delegate that constructs a <typeparamref name="TDbContext"/> from a <see cref="DbContextOptionsBuilder{TDbContext}"/>.
    /// This is called whenever a new DbContext is needed for database operations.
    /// Example: <c>builder => new MyDbContext(builder.Options)</c>
    /// </param>
    /// <param name="buildTemplate">
    /// A delegate that receives a <typeparamref name="TDbContext"/> and builds the template database schema. Optional.
    /// The DbContext is already configured with the connection.
    /// Called zero or once based on the current state of the underlying LocalDB:
    /// not called if a valid template already exists, called once if the template needs to be created or rebuilt.
    /// If null, only EnsureCreated() or migrations (depending on your setup) will be applied.
    /// Example: <c>async context => { await context.Database.EnsureCreatedAsync(); await context.SeedDataAsync(); }</c>
    /// </param>
    /// <param name="storage">
    /// Disk storage convention specifying the instance name and directory for .mdf and .ldf files. Optional.
    /// If not specified, defaults to a storage based on <typeparamref name="TDbContext"/> name.
    /// Use <see cref="Storage.FromSuffix{TDbContext}"/> to create a suffixed instance for parallel test runs.
    /// </param>
    /// <param name="timestamp">
    /// A timestamp used to determine if the template database needs to be rebuilt. Optional.
    /// If the timestamp is newer than the existing template, the template is recreated.
    /// Defaults to the last modified time of <paramref name="buildTemplate"/> or <typeparamref name="TDbContext"/> assembly.
    /// </param>
    /// <param name="templateSize">
    /// The initial size in MB for the template database. Optional. Defaults to 3 MB.
    /// Larger values may improve performance for databases with substantial initial data.
    /// </param>
    /// <param name="existingTemplate">
    /// Existing .mdf and .ldf files to use as the template instead of building one. Optional.
    /// When provided, <paramref name="buildTemplate"/> is not called and these files are used directly.
    /// Useful for scenarios where the template is pre-built or shared across test runs.
    /// </param>
    /// <param name="callback">
    /// A delegate executed after the template database has been created or mounted. Optional.
    /// Receives a <see cref="SqlConnection"/> and a <typeparamref name="TDbContext"/> instance.
    /// Useful for seeding reference data or performing post-creation setup that requires the context.
    /// Guaranteed to be called exactly once per <see cref="SqlInstance{TDbContext}"/> at startup.
    /// </param>
    /// <param name="sqlOptionsBuilder">
    /// An action to configure <see cref="SqlServerDbContextOptionsBuilder"/> for advanced SQL Server options. Optional.
    /// Passed to <see cref="SqlServerDbContextOptionsExtensions.UseSqlServer(DbContextOptionsBuilder,string,Action{SqlServerDbContextOptionsBuilder})"/>.
    /// Example: <c>options => options.EnableRetryOnFailure()</c>
    /// </param>
    /// <param name="shutdownTimeout">
    /// The number of seconds LocalDB waits before shutting down after the last connection closes. Optional.
    /// If not specified, defaults to <see cref="LocalDbSettings.ShutdownTimeout"/> (which can be configured
    /// via the <c>LocalDBShutdownTimeout</c> environment variable, defaulting to 5 minutes).
    /// </param>
    /// <param name="dbAutoOffline">
    /// Controls whether databases are automatically taken offline when disposed.
    /// When true, databases are taken offline (reduces memory). When false, databases remain online.
    /// When null (default), automatically enables offline mode if the CI environment variable is detected.
    /// </param>
    public SqlInstance(
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromContext<TDbContext>? buildTemplate = null,
        Storage? storage = null,
        DateTime? timestamp = null,
        ushort templateSize = 3,
        ExistingTemplate? existingTemplate = null,
        Callback<TDbContext>? callback = null,
        Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder = null,
        ushort? shutdownTimeout = null,
        bool? dbAutoOffline = null) :
        this(
            constructInstance,
            BuildTemplateConverter.Convert(constructInstance, buildTemplate),
            storage,
            GetTimestamp(timestamp, buildTemplate),
            templateSize,
            existingTemplate,
            callback,
            sqlOptionsBuilder,
            shutdownTimeout,
            dbAutoOffline)
    {
    }

    /// <summary>
    /// Instantiate a <see cref="SqlInstance{TDbContext}"/> using a connection-based template builder.
    /// Should usually be scoped as one instance per AppDomain, so all tests use the same instance.
    /// This overload provides direct access to the <see cref="SqlConnection"/> for advanced schema setup.
    /// </summary>
    /// <param name="constructInstance">
    /// A delegate that constructs a <typeparamref name="TDbContext"/> from a <see cref="DbContextOptionsBuilder{TDbContext}"/>.
    /// This is called whenever a new DbContext is needed for database operations.
    /// Example: <c>builder => new MyDbContext(builder.Options)</c>
    /// </param>
    /// <param name="buildTemplate">
    /// A delegate that receives a <see cref="SqlConnection"/> and <see cref="DbContextOptionsBuilder{TDbContext}"/> to build the template schema.
    /// The template is then cloned for each test.
    /// Called zero or once based on the current state of the underlying LocalDB:
    /// not called if a valid template already exists, called once if the template needs to be created or rebuilt.
    /// Provides direct SQL access plus the ability to configure DbContext options for schema creation.
    /// Example: <c>async (connection, builder) => { await using var context = new MyDbContext(builder.Options); await context.Database.MigrateAsync(); }</c>
    /// </param>
    /// <param name="storage">
    /// Disk storage convention specifying the instance name and directory for .mdf and .ldf files. Optional.
    /// If not specified, defaults to a storage based on <typeparamref name="TDbContext"/> name.
    /// Use <see cref="Storage.FromSuffix{TDbContext}"/> to create a suffixed instance for parallel test runs.
    /// </param>
    /// <param name="timestamp">
    /// A timestamp used to determine if the template database needs to be rebuilt. Optional.
    /// If the timestamp is newer than the existing template, the template is recreated.
    /// Defaults to the last modified time of <paramref name="buildTemplate"/> or <typeparamref name="TDbContext"/> assembly.
    /// </param>
    /// <param name="templateSize">
    /// The initial size in MB for the template database. Optional. Defaults to 3 MB.
    /// Larger values may improve performance for databases with substantial initial data.
    /// </param>
    /// <param name="existingTemplate">
    /// Existing .mdf and .ldf files to use as the template instead of building one. Optional.
    /// When provided, <paramref name="buildTemplate"/> is not called and these files are used directly.
    /// Useful for scenarios where the template is pre-built or shared across test runs.
    /// </param>
    /// <param name="callback">
    /// A delegate executed after the template database has been created or mounted. Optional.
    /// Receives a <see cref="SqlConnection"/> and a <typeparamref name="TDbContext"/> instance.
    /// Useful for seeding reference data or performing post-creation setup that requires the context.
    /// Guaranteed to be called exactly once per <see cref="SqlInstance{TDbContext}"/> at startup.
    /// </param>
    /// <param name="sqlOptionsBuilder">
    /// An action to configure <see cref="SqlServerDbContextOptionsBuilder"/> for advanced SQL Server options. Optional.
    /// Passed to <see cref="SqlServerDbContextOptionsExtensions.UseSqlServer(DbContextOptionsBuilder,string,Action{SqlServerDbContextOptionsBuilder})"/>.
    /// Example: <c>options => options.EnableRetryOnFailure()</c>
    /// </param>
    /// <param name="shutdownTimeout">
    /// The number of seconds LocalDB waits before shutting down after the last connection closes. Optional.
    /// If not specified, defaults to <see cref="LocalDbSettings.ShutdownTimeout"/> (which can be configured
    /// via the <c>LocalDBShutdownTimeout</c> environment variable, defaulting to 5 minutes).
    /// </param>
    /// <param name="dbAutoOffline">
    /// Controls whether databases are automatically taken offline when disposed.
    /// When true, databases are taken offline (reduces memory). When false, databases remain online.
    /// When null (default), automatically enables offline mode if the CI environment variable is detected.
    /// </param>
    public SqlInstance(
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromConnection<TDbContext> buildTemplate,
        Storage? storage = null,
        DateTime? timestamp = null,
        ushort templateSize = 3,
        ExistingTemplate? existingTemplate = null,
        Callback<TDbContext>? callback = null,
        Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder = null,
        ushort? shutdownTimeout = null,
        bool? dbAutoOffline = null)
    {
        if (!Guard.IsWindows)
        {
            return;
        }

        storage ??= defaultStorage;
        var resultTimestamp = GetTimestamp(timestamp, buildTemplate);
        Model = BuildModel(constructInstance, sqlOptionsBuilder);
        InitEntityMapping();
        this.constructInstance = constructInstance;
        this.sqlOptionsBuilder = sqlOptionsBuilder;
        this.dbAutoOffline = CiDetection.ResolveDbAutoOffline(dbAutoOffline);

        var storageValue = storage.Value;
        StorageDirectory = storageValue.Directory;
        DirectoryCleaner.CleanInstance(StorageDirectory);

        Task BuildTemplate(SqlConnection connection)
        {
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseSqlServer(connection, sqlOptionsBuilder);
            return buildTemplate(connection, builder);
        }

        Func<SqlConnection, Task>? wrapperCallback = null;
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
            storageValue.Name,
            StorageDirectory,
            templateSize,
            existingTemplate,
            wrapperCallback,
            shutdownTimeout);

        Wrapper.Start(resultTimestamp, BuildTemplate);
    }

    public string StorageDirectory { get; } = null!;

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

    static IModel BuildModel(ConstructInstance<TDbContext> constructInstance, Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder)
    {
        var builder = DefaultOptionsBuilder.Build<TDbContext>();
        builder.UseSqlServer("Fake", sqlOptionsBuilder);
        using var context = constructInstance(builder);
        return context.Model;
    }

    public string MasterConnectionString => Wrapper.MasterConnectionString;

    public void Dispose() =>
        Wrapper.Dispose();
}
