[TestFixture]
public class Tests
{
    static SqlInstance sqlInstance = new(
        name: "LoadTest",
        buildTemplate: CreateTable);

    [TestCaseSource(nameof(DatabaseNames))]
    public async Task Test(string name)
    {
        await using var database = await sqlInstance.Build(name);
        await AddData(database);
        var data = await GetData(database);
        AreEqual(1, data.Count);
    }

    static IEnumerable<string> DatabaseNames()
    {
        for (var i = 0; i < 1000; i++)
        {
            yield return $"Db{i}";
        }
    }

    static async Task CreateTable(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "create table MyTable (Value int);";
        await command.ExecuteNonQueryAsync();
    }

    static int intData;

    static async Task AddData(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        var addData = intData;
        intData++;
        command.CommandText =
            $"""
             insert into MyTable (Value)
             values ({addData});
             """;
        await command.ExecuteNonQueryAsync();
    }

    static async Task<List<int>> GetData(DbConnection connection)
    {
        var values = new List<int>();
        await using var command = connection.CreateCommand();
        command.CommandText = "select Value from MyTable";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetInt32(0));
        }

        return values;
    }
}