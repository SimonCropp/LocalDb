using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;

class ManifestTokenResolver :
    IManifestTokenResolver
{
    static DefaultManifestTokenResolver defaultResolver = new();

    public string ResolveManifestToken(DbConnection connection)
    {
        if (connection is SqlConnection)
        {
            return "2012";
        }

        return defaultResolver.ResolveManifestToken(connection);
    }
}