namespace EfLocalDb;

public partial class SqlInstance<TDbContext>
    where TDbContext : DbContext
{
    public async Task<SqlDatabase<TDbContext>> BuildShared(bool useTransaction = false)
    {
        Guard.AgainstBadOS();
        var connection = await Wrapper.OpenSharedDatabase();

        SqlTransaction? transaction = null;
        if (useTransaction)
        {
            transaction = (SqlTransaction) await connection.BeginTransactionAsync();
        }

        var database = new SqlDatabase<TDbContext>(
            this,
            connection,
            "Shared",
            constructInstance,
            () => Task.CompletedTask,
            null,
            null,
            sqlOptionsBuilder,
            readOnly: !useTransaction,
            transaction: transaction);
        await database.Start();
        return database;
    }

    public async Task<SqlDatabase<TDbContext>> BuildShared(
        IEnumerable<object>? data,
        bool useTransaction = false)
    {
        Guard.AgainstBadOS();

        Func<SqlConnection, Task>? initialize = null;
        if (data != null)
        {
            initialize = async initConnection =>
            {
                var builder = DefaultOptionsBuilder.Build<TDbContext>();
                builder.UseSqlServer(initConnection, sqlOptionsBuilder);
                await using var context = constructInstance(builder);
                await context.AddData(data, EntityTypes);
            };
        }

        var connection = await Wrapper.OpenSharedDatabase(initialize);

        SqlTransaction? transaction = null;
        if (useTransaction)
        {
            transaction = (SqlTransaction) await connection.BeginTransactionAsync();
        }

        var database = new SqlDatabase<TDbContext>(
            this,
            connection,
            "Shared",
            constructInstance,
            () => Task.CompletedTask,
            null,
            null,
            sqlOptionsBuilder,
            readOnly: !useTransaction,
            transaction: transaction);
        await database.Start();
        return database;
    }
}
