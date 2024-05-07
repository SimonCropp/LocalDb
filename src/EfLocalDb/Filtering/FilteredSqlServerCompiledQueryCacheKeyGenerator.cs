#pragma warning disable EF1001
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;

class FilteredSqlServerCompiledQueryCacheKeyGenerator(CompiledQueryCacheKeyGeneratorDependencies dependencies, RelationalCompiledQueryCacheKeyGeneratorDependencies relationalDependencies, ISqlServerConnection connection)
    : SqlServerCompiledQueryCacheKeyGenerator(dependencies, relationalDependencies, connection)
{
    public override object GenerateCacheKey(Expression query, bool async) =>
        new QueryFilterRespectingKey(base.GenerateCacheKey(query, async), ShouldIgnoreQueryFilter(query));

    static bool ShouldIgnoreQueryFilter(Expression expression)
    {
        if (expression is MethodCallExpression call)
        {
            var method = call.Method;
            return method.Name == "IgnoreQueryFilters" &&
                   method.DeclaringType == typeof(EntityFrameworkQueryableExtensions);
        }

        return false;
    }

    class QueryFilterRespectingKey(object inner, bool ignoreQueryFilter)
    {
        public override int GetHashCode()
            => HashCode.Combine(inner, ignoreQueryFilter);
    }
}