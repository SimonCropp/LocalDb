namespace LocalDb;

public partial class SqlInstance
{
    public async Task<SqlDatabase> BuildShared(bool useTransaction = false)
    {
        Guard.AgainstBadOS();
        var connection = await Wrapper.OpenSharedDatabase();

        SqlTransaction? transaction = null;
        if (useTransaction)
        {
#if NET5_0_OR_GREATER
            transaction = (SqlTransaction) await connection.BeginTransactionAsync();
#else
            transaction = connection.BeginTransaction();
#endif
        }

        Func<Task>? verifyNotModified = null;
        if (!useTransaction)
        {
            var size = Wrapper.GetSharedFileSize();
            verifyNotModified = () => Wrapper.ThrowIfSharedDatabaseModified(size);
        }

        return new(connection, "Shared", () => Task.CompletedTask, verifyNotModified, transaction);
    }
}
