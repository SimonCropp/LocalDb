#pragma warning disable EF1001
class QueryProvider(IQueryCompiler compiler) :
    EntityQueryProvider(compiler)
{
    public override IQueryable CreateQuery(Expression expression)
    {
        ThrowForRedundantIgnoreQueryFilters(expression);
        return base.CreateQuery(expression);
    }

    public override IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        ThrowForRedundantIgnoreQueryFilters(expression);
        return base.CreateQuery<TElement>(expression);
    }

    static void ThrowForRedundantIgnoreQueryFilters(Expression expression)
    {
        if (expression is not MethodCallExpression {Method.Name: "IgnoreQueryFilters"})
        {
            return;
        }

        if (!QueryFilter.IsDisabled)
        {
            return;
        }

        throw new("Query filters are already disabled. Call to IgnoreQueryFilters is redundant.");
    }
}