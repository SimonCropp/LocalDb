/// <summary>
/// Blocks writes on a read-only shared database.
/// </summary>
/// <remarks>
/// Two interception points are needed. The <see cref="SaveChangesInterceptor" /> base covers the
/// change tracker, while <see cref="Command" /> covers the paths that bypass it entirely:
/// <c>ExecuteUpdate</c>, <c>ExecuteDelete</c>, <c>ExecuteSqlRaw</c> and any hand-written command.
/// Those all run through <c>ExecuteNonQuery</c>, which a query never does, so blocking it needs no
/// inspection of the command text.
/// </remarks>
class ReadOnlyInterceptor :
    SaveChangesInterceptor
{
    public static readonly ReadOnlyInterceptor Instance = new();

    internal const string Message = "Writes are not supported on shared databases. Use [PooledDb] for tests that write, or BuildShared(useTransaction: true) when building a database directly.";

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
        throw new(Message);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, Cancel cancellationToken = default) =>
        throw new(Message);

    public class Command :
        DbCommandInterceptor
    {
        public static readonly Command Instance = new();

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result) =>
            throw new(Message);

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, Cancel cancellationToken = default) =>
            throw new(Message);
    }
}
