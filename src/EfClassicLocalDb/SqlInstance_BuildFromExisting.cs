namespace EfLocalDb;

public partial class SqlInstance<TDbContext>
    where TDbContext : DbContext
{
    public async Task<SqlDatabase<TDbContext>> BuildFromExisting(string dbName)
    {
        Guard.AgainstBadOS();
        Ensure.NotNullOrWhiteSpace(dbName);
        var connection = await Wrapper.OpenExistingDatabase(dbName);
        var database = new SqlDatabase<TDbContext>(
            connection,
            dbName,
            constructInstance,
            () => Task.CompletedTask,
            null,
            null);
        await database.Start();
        return database;
    }
}
