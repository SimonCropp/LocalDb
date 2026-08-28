namespace EfLocalDb;

public partial class SqlInstance<TDbContext>
    where TDbContext : DbContext
{
    /// <summary>
    /// Leases a database from a fixed pool built once from the template, instead of creating a
    /// database per test. Changes are wrapped in a transaction that is rolled back when the
    /// returned <see cref="SqlDatabase{TDbContext}" /> is disposed, which returns the database
    /// to the pool for the next test.
    /// <para>
    /// The win over <see cref="Build(string, string, string)" /> is that the query plan cache is
    /// keyed by database, so reusing a small set of databases lets plans be reused across tests
    /// rather than recompiled for every one. It also removes the per test file copy and attach.
    /// </para>
    /// <para>
    /// Pool size is <see cref="LocalDbSettings.PoolSize" />, and it bounds how many pooled tests
    /// run concurrently. Tests that need their changes committed, or that assert on state outside
    /// their own transaction, are not suited to this and should use
    /// <see cref="Build(string, string, string)" />.
    /// </para>
    /// </summary>
    /// <param name="useTransaction">
    /// When true (the default) writes are rolled back on dispose. When false the lease still
    /// works, but changes persist into the pooled database and leak to later tests.
    /// </param>
    public async Task<SqlDatabase<TDbContext>> BuildPooled(bool useTransaction = true)
    {
        Guard.AgainstBadOS();

        var (connection, name) = await Wrapper.OpenPooledDatabase();

        SqlTransaction? transaction = null;
        try
        {
            if (useTransaction)
            {
                transaction = (SqlTransaction) await connection.BeginTransactionAsync();
            }

            var database = new SqlDatabase<TDbContext>(
                this,
                connection,
                name,
                constructInstance,
                () => Task.CompletedTask,
                null,
                null,
                sqlOptionsBuilder,
                readOnly: false,
                transaction: transaction,
                released: () =>
                {
                    Wrapper.ReleasePooled(name);
                    return Task.CompletedTask;
                });
            await database.Start();
            return database;
        }
        catch
        {
            // Never strand a lease: without this a failure between lease and hand off would
            // permanently shrink the pool and eventually deadlock the run.
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }

            await connection.DisposeAsync();
            Wrapper.ReleasePooled(name);
            throw;
        }
    }
}
