static class Lambda<T>
{
    static ParameterExpression parameter = Expression.Parameter(typeof(T), "e");

    public static Expression<Func<T, bool>> Build(IReadOnlyList<IProperty> properties, ValueBuffer values)
    {
#pragma warning disable EF1001
        var predicate = Microsoft.EntityFrameworkCore.Internal.ExpressionExtensions.BuildPredicate(properties, values, parameter);
#pragma warning restore EF1001
        return Expression.Lambda<Func<T, bool>>(predicate, parameter);
    }
}