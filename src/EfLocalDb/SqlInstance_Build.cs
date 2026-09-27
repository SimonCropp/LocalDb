// ReSharper disable RedundantCast
namespace EfLocalDb;

public partial class SqlInstance<TDbContext>
    where TDbContext : DbContext
{
    const string buildContextRemoved = "Removed. Use Build with the same arguments and dispose the returned SqlDatabase. Use its Context, or NewConnectionOwnedDbContext() for a context with its own connection.";

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

    [Obsolete(buildContextRemoved, error: true)]
    public Task<TDbContext> BuildContext(
        IEnumerable<object>? data,
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "") =>
        throw new NotImplementedException();

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

    [Obsolete(buildContextRemoved, error: true)]
    public Task<TDbContext> BuildContext(
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "") =>
        throw new NotImplementedException();

    public async Task<SqlDatabase<TDbContext>> Build(
        string dbName,
        IEnumerable<object>? data)
    {
        Guard.AgainstBadOS();
        Ensure.NotNullOrWhiteSpace(dbName);
        var connection = await Wrapper.CreateDatabaseFromTemplate(dbName);
        var database = new SqlDatabase<TDbContext>(
            this,
            connection,
            dbName,
            constructInstance,
            () => Wrapper.DeleteDatabase(dbName),
            data,
            sqlOptionsBuilder);
        await database.Start();

        return database;
    }

    [Obsolete(buildContextRemoved, error: true)]
    public Task<TDbContext> BuildContext(
        string dbName,
        IEnumerable<object>? data) =>
        throw new NotImplementedException();

    public Task<SqlDatabase<TDbContext>> Build(string dbName)
    {
        Guard.AgainstBadOS();
        return Build(dbName, (IEnumerable<object>?) null);
    }

    [Obsolete(buildContextRemoved, error: true)]
    public Task<TDbContext> BuildContext(string dbName) =>
        throw new NotImplementedException();
}
