// ReSharper disable RedundantCast
namespace EfLocalDb;

public partial class SqlInstance<TDbContext>
    where TDbContext : DbContext
{
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
        Guard.AgainstNullWhiteSpace(testFile);
        Guard.AgainstNullWhiteSpace(memberName);
        Guard.AgainstWhiteSpace(databaseSuffix);

        var testClass = Path.GetFileNameWithoutExtension(testFile);

        var dbName = DbNamer.DeriveDbName(databaseSuffix, memberName, testClass);
        return Build(dbName, data);
    }

    public async Task<TDbContext> BuildContext(
        IEnumerable<object>? data,
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "")
    {
        Guard.AgainstBadOS();
        await using var build = await Build(data, testFile, databaseSuffix, memberName);
        return build.NewConnectionOwnedDbContext();
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

    public async Task<TDbContext> BuildContext(
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "")
    {
        Guard.AgainstBadOS();
        await using var build = await Build(testFile, databaseSuffix, memberName);
        return build.NewConnectionOwnedDbContext();
    }

    public async Task<SqlDatabase<TDbContext>> Build(
        string dbName,
        IEnumerable<object>? data)
    {
        Guard.AgainstBadOS();
        Guard.AgainstNullWhiteSpace(dbName);
        var connection = await BuildDatabase(dbName);
        var database = new SqlDatabase<TDbContext>(
            connection,
            dbName,
            constructInstance,
            () => Wrapper.DeleteDatabase(dbName),
            data,
            sqlOptionsBuilder);
        await database.Start();
        return database;
    }

    public async Task<TDbContext> BuildContext(
        string dbName,
        IEnumerable<object>? data)
    {
        Guard.AgainstBadOS();
        await using var build = await Build(dbName, data);
        return build.NewConnectionOwnedDbContext();
    }

    public Task<SqlDatabase<TDbContext>> Build(string dbName)
    {
        Guard.AgainstBadOS();
        return Build(dbName, (IEnumerable<object>?) null);
    }

    public async Task<TDbContext> BuildContext(string dbName)
    {
        Guard.AgainstBadOS();
        await using var build = await Build(dbName, (IEnumerable<object>?) null);
        return build.NewConnectionOwnedDbContext();
    }
}