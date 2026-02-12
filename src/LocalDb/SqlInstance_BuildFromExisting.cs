namespace LocalDb;

public partial class SqlInstance
{
    public async Task<SqlDatabase> BuildFromExisting(string dbName)
    {
        Guard.AgainstBadOS();
        Ensure.NotNullOrWhiteSpace(dbName);
        var connection = await Wrapper.OpenExistingDatabase(dbName);
        return new(connection, dbName, () => Task.CompletedTask);
    }
}
