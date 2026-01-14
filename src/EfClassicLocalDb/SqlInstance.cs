// ReSharper disable RedundantCast

namespace EfLocalDb;

public class SqlInstance<TDbContext> :
    IDisposable
    where TDbContext : DbContext
{
    internal Wrapper Wrapper { get; } = null!;
    ConstructInstance<TDbContext> constructInstance = null!;
    static Storage defaultStorage;

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

    public SqlInstance(
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromContext<TDbContext>? buildTemplate = null,
        Storage? storage = null,
        DateTime? timestamp = null,
        ushort templateSize = 3,
        ExistingTemplate? existingTemplate = null,
        Callback<TDbContext>? callback = null) :
        this(
            constructInstance,
            BuildTemplateConverter.Convert(constructInstance, buildTemplate),
            storage,
            GetTimestamp(timestamp, buildTemplate),
            templateSize,
            existingTemplate,
            callback)
    {
    }

    public SqlInstance(
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromConnection buildTemplate,
        Storage? storage = null,
        DateTime? timestamp = null,
        ushort templateSize = 3,
        ExistingTemplate? existingTemplate = null,
        Callback<TDbContext>? callback = null)
    {
        if (!Guard.IsWindows)
        {
            return;
        }

        storage ??= defaultStorage;

        var resultTimestamp = GetTimestamp(timestamp, buildTemplate);
        this.constructInstance = constructInstance;

        var storageValue = storage.Value;
        DirectoryCleaner.CleanInstance(storageValue.Directory);

        Func<SqlConnection, Cancel, Task>? wrapperCallback = null;
        if (callback is not null)
        {
            wrapperCallback = async (connection, cancel) =>
            {
                using var context = constructInstance(connection);
                await callback(connection, context, cancel);
            };
        }

        Wrapper = new(
            storageValue.Name,
            storageValue.Directory,
            templateSize,
            existingTemplate,
            wrapperCallback);
        Wrapper.Start(resultTimestamp, (connection, cancel) => buildTemplate(connection, cancel));
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
    /// <param name="cancel">The cancellation instruction.</param>
    public Task<SqlDatabase<TDbContext>> Build(
        IEnumerable<object>? data,
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "",
        Cancel cancel = default)
    {
        Guard.AgainstBadOS();
        Ensure.NotNullOrWhiteSpace(testFile);
        Ensure.NotNullOrWhiteSpace(memberName);
        Ensure.NotWhiteSpace(databaseSuffix);

        var testClass = Path.GetFileNameWithoutExtension(testFile);

        var dbName = DbNamer.DeriveDbName(databaseSuffix, memberName, testClass);
        return Build(dbName, data, cancel);
    }

    /// <summary>
    ///     Build DB with a name based on the calling Method.
    /// </summary>
    /// <param name="testFile">The path to the test class. Used to make the db name unique per test type.</param>
    /// <param name="databaseSuffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
    /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
    /// <param name="cancel">The cancellation instruction.</param>
    public Task<SqlDatabase<TDbContext>> Build(
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "",
        Cancel cancel = default)
    {
        Guard.AgainstBadOS();
        return Build(null, testFile, databaseSuffix, memberName, cancel);
    }

    public async Task<SqlDatabase<TDbContext>> Build(
        string dbName,
        IEnumerable<object>? data,
        Cancel cancel = default)
    {
        Guard.AgainstBadOS();
        Ensure.NotNullOrWhiteSpace(dbName);
        var connection = await Wrapper.CreateDatabaseFromTemplate(dbName, cancel);
        var database = new SqlDatabase<TDbContext>(
            connection,
            dbName,
            constructInstance,
            cancel => Wrapper.DeleteDatabase(dbName, cancel),
            data);
        await database.Start(cancel);
        return database;
    }

    public Task<SqlDatabase<TDbContext>> Build(string dbName, Cancel cancel = default)
    {
        Guard.AgainstBadOS();
        return Build(dbName, (IEnumerable<object>?) null, cancel);
    }

    public string MasterConnectionString => Wrapper.MasterConnectionString;

    public void Dispose() =>
        Wrapper.Dispose();
}
