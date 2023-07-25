static class Lambda<T>
{
    static ParameterExpression parameter = Expression.Parameter(typeof(T), "e");

    public static Expression<Func<T, bool>> Build(IReadOnlyList<IProperty> properties, ValueBuffer values)
    {
        var predicate = Microsoft.EntityFrameworkCore.Internal.ExpressionExtensions.BuildPredicate(properties, values, parameter);
        return Expression.Lambda<Func<T, bool>>(predicate, parameter);
    }
}