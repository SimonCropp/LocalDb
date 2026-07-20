[TestFixture]
public class InstanceDiagnosticsTests
{
    [Test]
    public async Task DisabledOnNewInstance()
    {
        var name = "InstanceDiagnosticsTest";
        LocalDbApi.StopAndDelete(name);
        DirectoryFinder.Delete(name);

        // a new instance runs the optimize commands, an existing one does not
        using var wrapper = new Wrapper(name, DirectoryFinder.Find(name));
        wrapper.Start(new(2000, 1, 1), TestDbBuilder.CreateTable);
        await wrapper.AwaitStart();

        try
        {
            await using var connection = new SqlConnection(wrapper.MasterConnectionString);
            await connection.OpenAsync();

            AreEqual(
                0,
                await Scalar(connection, "select value_in_use from sys.configurations where name = 'default trace enabled'"));
            AreEqual(
                0,
                await Scalar(connection, "select count(*) from sys.dm_xe_sessions where name = 'system_health'"));
            AreEqual(
                0,
                await Scalar(connection, "select startup_state from sys.server_event_sessions where name = 'system_health'"));
        }
        finally
        {
            DirectoryCleaner.RemoveInstance(name);
            DirectoryFinder.Delete(name);
        }
    }

    static async Task<int> Scalar(SqlConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
}
