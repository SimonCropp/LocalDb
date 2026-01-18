#pragma warning disable EF1001
// ReSharper disable once ClassNeverInstantiated.Global
class KeyGenerator(CompiledQueryCacheKeyGeneratorDependencies dependencies, RelationalCompiledQueryCacheKeyGeneratorDependencies relationalDependencies, ISqlServerConnection connection)
    : SqlServerCompiledQueryCacheKeyGenerator(dependencies, relationalDependencies, connection)
{
    readonly ISqlServerConnection connection = connection;

    public override object GenerateCacheKey(Expression query, bool async)
        => new SqlServerCompiledQueryCacheKey(
            GenerateCacheKeyCore(query, async),
            connection.IsMultipleActiveResultSetsEnabled,
            QueryFilter.IsEnabled);

    readonly struct SqlServerCompiledQueryCacheKey(
        RelationalCompiledQueryCacheKey relationalKey,
        bool mars,
        bool queryFilterEnabled)
        : IEquatable<SqlServerCompiledQueryCacheKey>
    {
        readonly RelationalCompiledQueryCacheKey relationalKey = relationalKey;
        readonly bool mars = mars;
        readonly bool queryFilterEnabled = queryFilterEnabled;

        public override bool Equals(object? obj)
            => obj is SqlServerCompiledQueryCacheKey key &&
               Equals(key);

        public bool Equals(SqlServerCompiledQueryCacheKey other)
            => relationalKey.Equals(other.relationalKey) &&
               mars == other.mars &&
               queryFilterEnabled == other.queryFilterEnabled;

        public override int GetHashCode()
            => HashCode.Combine(relationalKey, mars, queryFilterEnabled);
    }
}
