namespace LocalDb;

/// <summary>
/// Manages the lifecycle of a SQL Server LocalDB instance for testing purposes.
/// Provides template-based database creation for efficient test isolation.
/// </summary>
public class SqlInstance :
    IDisposable
{
    internal readonly Wrapper Wrapper = null!;
    bool dbAutoOffline;

    public string ServerName => Wrapper.ServerName;

    /// <summary>
    /// Instantiate a new <see cref="SqlInstance"/>.
    /// Should usually be scoped as one instance per AppDomain, so all tests use the same instance.
    /// </summary>
    /// <param name="name">
    /// The name of the SQL LocalDB instance. Used to identify the instance and derive
    /// storage paths if <paramref name="directory"/> is not specified.
    /// </param>
    /// <param name="buildTemplate">
    /// A delegate that receives a <see cref="SqlConnection"/> and builds the template database schema.
    /// The template is then cloned for each test.
    /// Called zero or once based on the current state of the underlying LocalDB:
    /// not called if a valid template already exists, called once if the template needs to be created or rebuilt.
    /// Example: <c>async connection => { await using var cmd = connection.CreateCommand(); ... }</c>
    /// </param>
    /// <param name="directory">
    /// The directory where the .mdf and .ldf database files will be stored. Optional.
    /// If not specified, a directory is derived based on <paramref name="name"/>.
    /// </param>
    /// <param name="timestamp">
    /// A timestamp used to determine if the template database needs to be rebuilt. Optional.
    /// If the timestamp is newer than the existing template, the template is recreated.
    /// Defaults to the last modified time of the assembly containing <paramref name="buildTemplate"/>.
    /// </param>
    /// <param name="templateSize">
    /// The initial size in MB for the template database. Optional. Defaults to 3 MB.
    /// Larger values may improve performance for databases with substantial initial data.
    /// </param>
    /// <param name="exitingTemplate">
    /// Existing .mdf and .ldf files to use as the template instead of building one. Optional.
    /// When provided, <paramref name="buildTemplate"/> is not called and these files are used directly.
    /// Useful for scenarios where the template is pre-built or shared across test runs.
    /// </param>
    /// <param name="callback">
    /// A delegate executed after the template database has been created or mounted. Optional.
    /// Receives a <see cref="SqlConnection"/> to the template database.
    /// Useful for seeding reference data or performing post-creation setup.
    /// Guaranteed to be called exactly once per <see cref="SqlInstance"/> at startup.
    /// </param>
    /// <param name="shutdownTimeout">
    /// The number of seconds LocalDB waits before shutting down after the last connection closes. Optional.
    /// If not specified, defaults to <see cref="LocalDbSettings.ShutdownTimeout"/> (which can be configured
    /// via the <c>LocalDBShutdownTimeout</c> environment variable, defaulting to 5 minutes).
    /// </param>
    /// <param name="dbAutoOffline">
    /// Controls whether databases are automatically taken offline when disposed.
    /// When true, databases are taken offline (reduces memory). When false, databases remain online.
    /// If not specified, defaults to <see cref="LocalDbSettings.DBAutoOffline"/> (which can be configured
    /// via the <c>LocalDBAutoOffline</c> environment variable, defaulting to auto-detection based on CI environment).
    /// </param>
    public SqlInstance(
        string name,
        Func<SqlConnection, Task> buildTemplate,
        string? directory = null,
        DateTime? timestamp = null,
        ushort templateSize = 3,
        ExistingTemplate? exitingTemplate = null,
        Func<SqlConnection, Task>? callback = null,
        ushort? shutdownTimeout = null,
        bool? dbAutoOffline = null)
    {
        if (!Guard.IsWindows)
        {
            return;
        }

        Ensure.NotNullOrWhiteSpace(name);
        if (directory == null)
        {
            directory = DirectoryFinder.Find(name);
        }
        else
        {
            Ensure.NotWhiteSpace(directory);
        }

        this.dbAutoOffline = CiDetection.ResolveDbAutoOffline(dbAutoOffline);
        DirectoryCleaner.CleanInstance(directory);
        var callingAssembly = Assembly.GetCallingAssembly();
        var resultTimestamp = GetTimestamp(timestamp, buildTemplate, callingAssembly);

        Wrapper = new(name, directory, templateSize, exitingTemplate, callback, shutdownTimeout);
        Wrapper.Start(resultTimestamp, buildTemplate);
    }

    static DateTime GetTimestamp(DateTime? timestamp, Delegate? buildTemplate, Assembly callingAssembly)
    {
        if (timestamp is not null)
        {
            return timestamp.Value;
        }

        if (buildTemplate is not null)
        {
            return Timestamp.LastModified(buildTemplate);
        }

        return Timestamp.LastModified(callingAssembly);
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

    #region ConventionBuildSignature

    /// <summary>
    ///     Build database with a name based on the calling Method.
    /// </summary>
    /// <param name="testFile">
    ///     The path to the test class.
    ///     Used to make the database name unique per test type.
    /// </param>
    /// <param name="databaseSuffix">
    ///     For Xunit theories add some text based on the inline data
    ///     to make the db name unique.
    /// </param>
    /// <param name="memberName">
    ///     Used to make the db name unique per method.
    ///     Will default to the caller method name is used.
    /// </param>
    public Task<SqlDatabase> Build(
            [CallerFilePath] string testFile = "",
            string? databaseSuffix = null,
            [CallerMemberName] string memberName = "")

        #endregion

    {
        Guard.AgainstBadOS();
        Ensure.NotNullOrWhiteSpace(testFile);
        Ensure.NotNullOrWhiteSpace(memberName);
        Ensure.NotWhiteSpace(databaseSuffix);

        var testClass = Path.GetFileNameWithoutExtension(testFile);

        var name = DbNamer.DeriveDbName(databaseSuffix, memberName, testClass);

        return Build(name);
    }

    #region ExplicitBuildSignature

    /// <summary>
    ///     Build database with an explicit name.
    /// </summary>
    public async Task<SqlDatabase> Build(string dbName)

        #endregion

    {
        Guard.AgainstBadOS();
        Ensure.NotNullOrWhiteSpace(dbName);
        var connection = await Wrapper.CreateDatabaseFromTemplate(dbName);
        Func<Task>? takeOffline = dbAutoOffline ? () => Wrapper.TakeOffline(dbName) : null;
        return new(connection, dbName, () => Wrapper.DeleteDatabase(dbName), takeOffline);
    }

    public string MasterConnectionString => Wrapper.MasterConnectionString;

    public void Dispose() =>
        Wrapper.Dispose();
}
