class TakeOfflineInterceptor(Func<Task> takeOffline) :
    DbConnectionInterceptor
{
    public override async Task ConnectionDisposedAsync(DbConnection connection, ConnectionEndEventData eventData)
    {
        await base.ConnectionDisposedAsync(connection, eventData);
        await takeOffline();
    }

    public override void ConnectionDisposed(DbConnection connection, ConnectionEndEventData eventData)
    {
        base.ConnectionDisposed(connection, eventData);
        takeOffline().GetAwaiter().GetResult();
    }
}
