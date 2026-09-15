// Decides whether a LocalDbTestBase test runs against a new, shared, or pooled database.
// [NewDb], [SharedDb] and [PooledDb] can be applied to the test method or the test class, and
// [SharedDb] and [PooledDb] also to the assembly. The nearest wins, and a new database is the
// default. Every level is validated, so two attributes on one level throw even when a nearer level
// decides. The attribute types are type parameters because each test framework package compiles
// its own copy of them.
static class DbAttributeReader
{
    public static DbMode Read<TShared, TPooled, TNew>(MethodInfo method, Type type)
        where TShared : Attribute
        where TPooled : Attribute
        where TNew : Attribute =>
        Read<TShared, TPooled, TNew>(method, type, type.Assembly);

    public static DbMode Read<TShared, TPooled, TNew>(MethodInfo method, Type type, Assembly assembly)
        where TShared : Attribute
        where TPooled : Attribute
        where TNew : Attribute
    {
        var methodMode = ReadLevel(
            method.GetCustomAttribute<TShared>() != null,
            method.GetCustomAttribute<TPooled>() != null,
            method.GetCustomAttribute<TNew>() != null,
            "a test method");
        var classMode = ReadLevel(
            type.GetCustomAttribute<TShared>() != null,
            type.GetCustomAttribute<TPooled>() != null,
            type.GetCustomAttribute<TNew>() != null,
            "a test class");
        var assemblyMode = ReadLevel(
            assembly.GetCustomAttribute<TShared>() != null,
            assembly.GetCustomAttribute<TPooled>() != null,
            assembly.GetCustomAttribute<TNew>() != null,
            "an assembly");
        return methodMode ?? classMode ?? assemblyMode ?? DbMode.New;
    }

    static DbMode? ReadLevel(bool isShared, bool isPooled, bool isNew, string target)
    {
        if ((isShared && isPooled) ||
            (isShared && isNew) ||
            (isPooled && isNew))
        {
            throw new($"[PooledDb], [SharedDb] and [NewDb] are mutually exclusive. Use only one on {target}.");
        }

        if (isShared)
        {
            return DbMode.Shared;
        }

        if (isPooled)
        {
            return DbMode.Pooled;
        }

        if (isNew)
        {
            return DbMode.New;
        }

        return null;
    }
}
