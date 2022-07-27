using System.Data.Common;
using System.Data.Entity.Infrastructure;

class ManifestTokenResolver :
    IManifestTokenResolver
{
    public string ResolveManifestToken(DbConnection connection) =>
        "2012";
}