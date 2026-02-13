class ReadOnlyInterceptor :
    SaveChangesInterceptor
{
    public static readonly ReadOnlyInterceptor Instance = new();

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
        throw new("Writes are not supported on shared databases. Use useTransaction: true to allow writes.");

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, Cancel cancellationToken = default) =>
        throw new("Writes are not supported on shared databases. Use useTransaction: true to allow writes.");
}
