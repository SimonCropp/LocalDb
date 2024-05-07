#pragma warning disable EF1001
using EfLocalDb;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;

class FilteredCompilationContextFactory(QueryCompilationContextDependencies dependencies, RelationalQueryCompilationContextDependencies relationalDependencies, ISqlServerConnection connection)
    : SqlServerQueryCompilationContextFactory(dependencies, relationalDependencies, connection)
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<IgnoreQueryFilters>k__BackingField")]
    private static extern ref bool IgnoreQueryFilters(QueryCompilationContext context);

    public override QueryCompilationContext Create(bool async)
    {
        var context = base.Create(async);

        if (QueryFilter.IsDisabled)
        {
            IgnoreQueryFilters(context) = true;
        }

        return context;
    }
}