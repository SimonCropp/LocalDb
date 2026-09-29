// Decides whether a LocalDbTestBase test runs against a new, shared, or pooled database, or none.
// [NewDb], [SharedDb], [PooledDb] and [NoDb] can be applied to the test method or the test class,
// and [SharedDb] and [PooledDb] also to the assembly. The nearest wins, and a new database is the
// default. Every level is validated, so two attributes on one level throw even when a nearer level
// decides. The attribute types are type parameters because each test framework package compiles
// its own copy of them.
static class DbAttributeReader
{
    public static DbMode Read<TShared, TPooled, TNew, TNo>(MethodInfo method, Type type)
        where TShared : Attribute
        where TPooled : Attribute
        where TNew : Attribute
        where TNo : Attribute =>
        Read<TShared, TPooled, TNew, TNo>(method, type, type.Assembly);

    public static DbMode Read<TShared, TPooled, TNew, TNo>(MethodInfo method, Type type, Assembly assembly)
        where TShared : Attribute
        where TPooled : Attribute
        where TNew : Attribute
        where TNo : Attribute
    {
        var methodMode = ReadLevel<TShared, TPooled, TNew, TNo>(method, "a test method");
        var classMode = ReadLevel<TShared, TPooled, TNew, TNo>(type, "a test class");
        var assemblyMode = ReadLevel<TShared, TPooled, TNew, TNo>(assembly, "an assembly");
        return methodMode ?? classMode ?? assemblyMode ?? DbMode.New;
    }

    static DbMode? ReadLevel<TShared, TPooled, TNew, TNo>(ICustomAttributeProvider provider, string target)
        where TShared : Attribute
        where TPooled : Attribute
        where TNew : Attribute
        where TNo : Attribute
    {
        var modes = new List<DbMode>();
        if (provider.IsDefined(typeof(TShared), true))
        {
            modes.Add(DbMode.Shared);
        }

        if (provider.IsDefined(typeof(TPooled), true))
        {
            modes.Add(DbMode.Pooled);
        }

        if (provider.IsDefined(typeof(TNew), true))
        {
            modes.Add(DbMode.New);
        }

        if (provider.IsDefined(typeof(TNo), true))
        {
            modes.Add(DbMode.None);
        }

        if (modes.Count > 1)
        {
            throw new($"[PooledDb], [SharedDb], [NewDb] and [NoDb] are mutually exclusive. Use only one on {target}.");
        }

        if (modes.Count == 1)
        {
            return modes[0];
        }

        return null;
    }
}
