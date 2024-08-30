namespace TestBase;

#region TestBase

public abstract class TestBase
{
    static SqlInstance instance;

    static TestBase() =>
        instance = new(
            name: "TestBaseUsage",
            buildTemplate: TestDbBuilder.CreateTable);

    public static Task<SqlDatabase> LocalDb(
        [CallerFilePath] string testFile = "",
        string? databaseSuffix = null,
        [CallerMemberName] string memberName = "") =>
        instance.Build(testFile, databaseSuffix, memberName);
}

public class Tests :
    TestBase
{
    [Test]
    public async Task Test()
    {
        await using var database = await LocalDb();
        await TestDbBuilder.AddData(database);
        var data = await TestDbBuilder.GetData(database);
        AreEqual(1, data.Count);
    }
}

#endregion