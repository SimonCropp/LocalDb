using System.Collections;

namespace EfLocalDb;

// AsSplitQuery only changes how collections are loaded. So only apply it when an Include loads a collection,
// since a query that loads no collection is a single query either way (and Verify.EntityFramework throws for it).
static class QuerySplitting
{
    public static IQueryable<TEntity> SplitIfIncludesCollection<TEntity>(IQueryable<TEntity> source)
        where TEntity : class
    {
        if (IncludesCollection(source.Expression))
        {
            return source.AsSplitQuery();
        }

        return source;
    }

    static bool IncludesCollection(Expression expression)
    {
        while (expression is MethodCallExpression
               {
                   Method.IsStatic: true,
                   Arguments.Count: > 0
               } call)
        {
            if (IsInclude(call.Method) &&
                call.Arguments.Count > 1)
            {
                var argument = Unquote(call.Arguments[1]);
                // a string Include can not be checked without resolving the path, so it is assumed to be a collection
                if (argument is not LambdaExpression lambda ||
                    IsCollection(lambda.Body.Type))
                {
                    return true;
                }
            }

            expression = call.Arguments[0];
        }

        return false;
    }

    static Expression Unquote(Expression expression)
    {
        while (expression is UnaryExpression
               {
                   NodeType: ExpressionType.Quote
               } unary)
        {
            expression = unary.Operand;
        }

        return expression;
    }

    static bool IsInclude(MethodInfo method) =>
        method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) &&
        method.Name is
            nameof(EntityFrameworkQueryableExtensions.Include) or
            nameof(EntityFrameworkQueryableExtensions.ThenInclude);

    static bool IsCollection(Type type) =>
        type != typeof(string) &&
        typeof(IEnumerable).IsAssignableFrom(type);
}
