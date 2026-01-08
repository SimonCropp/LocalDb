namespace LocalDb;

public class SqlInstance
{
    internal readonly Wrapper Wrapper = null!;

    public string ServerName => Wrapper.ServerName;

    public SqlInstance(
        string name,
        Func<DbConnection, Task> buildTemplate,
        string? directory = null,
        DateTime? timestamp = null,
        ushort templateSize = 3,
        ExistingTemplate? exitingTemplate = null,
        Func<DbConnection, Task>? callback = null,
        Func<string, DbConnection>? buildConnection = null)
    {
        if (!Guard.IsWindows)
        {
            return;
        }

        Ensure.NotWhiteSpace(directory);
        Ensure.NotNullOrWhiteSpace(name);
        directory = DirectoryFinder.Find(name);
        DirectoryCleaner.CleanInstance(directory);
        var callingAssembly = Assembly.GetCallingAssembly();
        var resultTimestamp = GetTimestamp(timestamp, buildTemplate, callingAssembly);
        buildConnection ??= _ => new SqlConnection(_);

        Wrapper = new(buildConnection, name, directory, templateSize, exitingTemplate, callback);
        Wrapper.Start(resultTimestamp, buildTemplate);
    }

    public SqlInstance(
        string name,
        Func<SqlConnection, Task> buildTemplate,
        string? directory = null,
        DateTime? timestamp = null,
        ushort templateSize = 3,
        ExistingTemplate? exitingTemplate = null,
        Func<SqlConnection, Task>? callback = null) :
        this(
            name,
            connection => buildTemplate((SqlConnection) connection),
            directory,
            timestamp,
            templateSize,
            exitingTemplate,
            connection =>
            {
                if (callback == null)
                {
                    return Task.CompletedTask;
                }

                return callback.Invoke((SqlConnection) connection);
            },
            _ => new SqlConnection(_))
    {
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

    Task<string> BuildContext(string dbName) => Wrapper.CreateDatabaseFromTemplate(dbName);

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
        var connection = await BuildContext(dbName);
        var database = new SqlDatabase(connection, dbName, () => Wrapper.DeleteDatabase(dbName));
        await database.Start();
        return database;
    }

    public string MasterConnectionString => Wrapper.MasterConnectionString;
}