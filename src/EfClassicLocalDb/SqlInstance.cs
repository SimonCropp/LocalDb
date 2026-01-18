// ReSharper disable RedundantCast

namespace EfLocalDb;

/// <summary>
/// Manages the lifecycle of a SQL Server LocalDB instance for Entity Framework 6 (Classic) testing.
/// Provides template-based database creation for efficient test isolation with DbContext integration.
/// </summary>
/// <typeparam name="TDbContext">The Entity Framework DbContext type used for database operations.</typeparam>
public class SqlInstance<TDbContext> :
    IDisposable
    where TDbContext : DbContext
{
    internal Wrapper Wrapper { get; } = null!;
    ConstructInstance<TDbContext> constructInstance = null!;
    static Storage defaultStorage;
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

    public string ServerName => Wrapper.ServerName;

    /// <summary>
    /// Instantiate a <see cref="SqlInstance{TDbContext}"/> using a context-based template builder.
    /// Should usually be scoped as one instance per AppDomain, so all tests use the same instance.
    /// </summary>
    /// <param name="constructInstance">
    /// A delegate that constructs a <typeparamref name="TDbContext"/> from a <see cref="SqlConnection"/>.
    /// This is called whenever a new DbContext is needed for database operations.
    /// Example: <c>connection => new MyDbContext(connection, contextOwnsConnection: false)</c>
    /// </param>
    /// <param name="buildTemplate">
    /// A delegate that receives a <typeparamref name="TDbContext"/> and builds the template database schema. Optional.
    /// The DbContext is already configured with the connection.
    /// Called zero or once based on the current state of the underlying LocalDB:
    /// not called if a valid template already exists, called once if the template needs to be created or rebuilt.
    /// If null, only EnsureCreated() or migrations (depending on your setup) will be applied.
    /// Example: <c>async context => { context.Database.CreateIfNotExists(); await context.SeedDataAsync(); }</c>
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
    /// A delegate that constructs a <typeparamref name="TDbContext"/> from a <see cref="SqlConnection"/>.
    /// This is called whenever a new DbContext is needed for database operations.
    /// Example: <c>connection => new MyDbContext(connection, contextOwnsConnection: false)</c>
    /// </param>
    /// <param name="buildTemplate">
    /// A delegate that receives a <see cref="SqlConnection"/> and builds the template database schema.
    /// The template is then cloned for each test.
    /// Called zero or once based on the current state of the underlying LocalDB:
    /// not called if a valid template already exists, called once if the template needs to be created or rebuilt.
    /// Useful when you need direct SQL access for schema creation, such as running migration scripts.
    /// Example: <c>async connection => { await using var cmd = connection.CreateCommand(); cmd.CommandText = "CREATE TABLE..."; await cmd.ExecuteNonQueryAsync(); }</c>
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
        TemplateFromConnection buildTemplate,
        Storage? storage = null,
        DateTime? timestamp = null,
        ushort templateSize = 3,
        ExistingTemplate? existingTemplate = null,
        Callback<TDbContext>? callback = null,
        ushort? shutdownTimeout = null,
        bool? dbAutoOffline = null)
    {
        if (!Guard.IsWindows)
        {
            return;
        }

        storage ??= defaultStorage;

        var resultTimestamp = GetTimestamp(timestamp, buildTemplate);
        this.constructInstance = constructInstance;
        this.dbAutoOffline = CiDetection.ResolveDbAutoOffline(dbAutoOffline);

        var storageValue = storage.Value;
        DirectoryCleaner.CleanInstance(storageValue.Directory);

        Func<SqlConnection, Task>? wrapperCallback = null;
        if (callback is not null)
        {
            wrapperCallback = async connection =>
            {
                using var context = constructInstance(connection);
                await callback(connection, context);
            };
        }

        Wrapper = new(
            storageValue.Name,
            storageValue.Directory,
            templateSize,
            existingTemplate,
            wrapperCallback,
            shutdownTimeout);
        Wrapper.Start(resultTimestamp, connection => buildTemplate(connection));
    }

    static DateTime GetTimestamp(DateTime? timestamp, Delegate? buildTemplate)
    {
        if (timestamp is not null)
        {
            return timestamp.Value;
        }

        if (buildTemplate is not null)
        {
            return Timestamp.LastModified(buildTemplate);
        }

        return Timestamp.LastModified<TDbContext>();
    }

    public void Cleanup()
    {
        Guard.AgainstBadOS();
        Wrapper.DeleteInstance();
    }

    public void Cleanup(ShutdownMode mode)
    {
        Guard.AgainstBadOS();
        Wrapper.DeleteInstance(mode);
    }

    public void Cleanup(ShutdownMode mode, TimeSpan timeout)
    {
        Guard.AgainstBadOS();
        Wrapper.DeleteInstance(mode, timeout);
    }

    /// <summary>
    ///     Build DB with a name based on the calling Method.
    /// </summary>
    /// <param name="data">The seed data.</param>
    /// <param name="testFile">The path to the test class. Used to make the db name unique per test type.</param>
    /// <param name="databaseSuffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
    /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
    public Task<SqlDatabase<TDbContext>> Build(
        IEnumerable<object>? data,
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "")
    {
        Guard.AgainstBadOS();
        Ensure.NotNullOrWhiteSpace(testFile);
        Ensure.NotNullOrWhiteSpace(memberName);
        Ensure.NotWhiteSpace(databaseSuffix);

        var testClass = Path.GetFileNameWithoutExtension(testFile);

        var dbName = DbNamer.DeriveDbName(databaseSuffix, memberName, testClass);
        return Build(dbName, data);
    }

    /// <summary>
    ///     Build DB with a name based on the calling Method.
    /// </summary>
    /// <param name="testFile">The path to the test class. Used to make the db name unique per test type.</param>
    /// <param name="databaseSuffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
    /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
    public Task<SqlDatabase<TDbContext>> Build(
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "")
    {
        Guard.AgainstBadOS();
        return Build(null, testFile, databaseSuffix, memberName);
    }

    public async Task<SqlDatabase<TDbContext>> Build(
        string dbName,
        IEnumerable<object>? data)
    {
        Guard.AgainstBadOS();
        Ensure.NotNullOrWhiteSpace(dbName);
        var connection = await Wrapper.CreateDatabaseFromTemplate(dbName);
        Func<Task>? takeOffline = dbAutoOffline ? () => Wrapper.TakeOffline(dbName) : null;
        var database = new SqlDatabase<TDbContext>(
            connection,
            dbName,
            constructInstance,
            () => Wrapper.DeleteDatabase(dbName),
            takeOffline,
            data);
        await database.Start();
        return database;
    }

    public Task<SqlDatabase<TDbContext>> Build(string dbName)
    {
        Guard.AgainstBadOS();
        return Build(dbName, (IEnumerable<object>?) null);
    }

    public string MasterConnectionString => Wrapper.MasterConnectionString;

    public void Dispose() =>
        Wrapper.Dispose();
}
