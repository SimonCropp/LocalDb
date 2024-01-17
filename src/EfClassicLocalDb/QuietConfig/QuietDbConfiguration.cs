namespace EfLocalDb;

public abstract class QuietDbConfiguration :
    DbConfiguration
{
    static MethodInfo databaseInitializerMethod = typeof(DbConfiguration)
        .GetMethod(
            nameof(SetDatabaseInitializer),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    protected QuietDbConfiguration()
    {
        var contextTypes = GetType().Assembly.GetTypes()
            .Where(_ => !_.IsAbstract && typeof(DbContext).IsAssignableFrom(_));
        foreach (var contextType in contextTypes)
        {
            var genericMethod = databaseInitializerMethod.MakeGenericMethod(contextType);
            genericMethod.Invoke(this, [null]);
        }

        SetManifestTokenResolver(new ManifestTokenResolver());
    }
}