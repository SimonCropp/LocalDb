using EfLocalDb;

namespace TestBase;

#region EfTestBase

public abstract class TestBase
{
    static SqlInstance<TheDbContext> sqlInstance;

    static TestBase() =>
        sqlInstance = new(
            constructInstance: builder => new(builder.Options));

    public static Task<SqlDatabase<TheDbContext>> LocalDb(
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "") =>
        sqlInstance.Build(testFile, databaseSuffix, memberName);
}

public class Tests :
    TestBase
{
    [Fact]
    public async Task Test()
    {
        await using var database = await LocalDb();
        var entity = new TheEntity
        {
            Property = "prop"
        };
        await database.AddData(entity);

        Assert.Single(database.Context.TestEntities);
    }
}

#endregion