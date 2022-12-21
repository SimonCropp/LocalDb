using System.Data.Entity.Infrastructure;

class ManifestTokenResolver :
    IManifestTokenResolver
{
    static DefaultManifestTokenResolver defaultResolver = new();

    public string ResolveManifestToken(DbConnection connection)
    {
        if (connection is DataSqlConnection)
        {
            return "2012";
        }

        return defaultResolver.ResolveManifestToken(connection);
    }
}