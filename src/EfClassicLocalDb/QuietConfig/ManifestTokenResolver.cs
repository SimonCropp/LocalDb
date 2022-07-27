using System.Data.Common;
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
            return defaultResolver.ResolveManifestToken(connection);
        }

        return "2012";
    }
}