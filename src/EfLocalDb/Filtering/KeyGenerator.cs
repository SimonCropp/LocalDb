#pragma warning disable EF1001
using EfLocalDb;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;

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
        RelationalCompiledQueryCacheKey relationalCompiledQueryCacheKey,
        bool multipleActiveResultSetsEnabled,
        bool queryFilterEnabled)
        : IEquatable<SqlServerCompiledQueryCacheKey>
    {
        readonly RelationalCompiledQueryCacheKey relationalCompiledQueryCacheKey = relationalCompiledQueryCacheKey;
        readonly bool multipleActiveResultSetsEnabled = multipleActiveResultSetsEnabled;
        readonly bool queryFilterEnabled = queryFilterEnabled;

        public override bool Equals(object? obj)
            => obj is SqlServerCompiledQueryCacheKey sqlServerCompiledQueryCacheKey &&
               Equals(sqlServerCompiledQueryCacheKey);

        public bool Equals(SqlServerCompiledQueryCacheKey other)
            => relationalCompiledQueryCacheKey.Equals(other.relationalCompiledQueryCacheKey) &&
               multipleActiveResultSetsEnabled == other.multipleActiveResultSetsEnabled &&
               queryFilterEnabled == other.queryFilterEnabled;

        public override int GetHashCode()
            => HashCode.Combine(relationalCompiledQueryCacheKey, multipleActiveResultSetsEnabled, queryFilterEnabled);
    }
}