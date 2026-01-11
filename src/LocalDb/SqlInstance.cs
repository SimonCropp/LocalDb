namespace LocalDb;

public class SqlInstance :
    IDisposable
{
    internal readonly Wrapper Wrapper = null!;

    public string ServerName => Wrapper.ServerName;

    public SqlInstance(
        string name,
        Func<SqlConnection, Task> buildTemplate,
        string? directory = null,
        DateTime? timestamp = null,
        ushort templateSize = 3,
        ExistingTemplate? exitingTemplate = null,
        Func<SqlConnection, Task>? callback = null)
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

        DirectoryCleaner.CleanInstance(directory);
        var callingAssembly = Assembly.GetCallingAssembly();
        var resultTimestamp = GetTimestamp(timestamp, buildTemplate, callingAssembly);

        Wrapper = new(name, directory, templateSize, exitingTemplate, callback);
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
        return new(connection, dbName, () => Wrapper.DeleteDatabase(dbName));
    }

    public string MasterConnectionString => Wrapper.MasterConnectionString;

    public void Dispose() =>
        Wrapper.Dispose();
}
