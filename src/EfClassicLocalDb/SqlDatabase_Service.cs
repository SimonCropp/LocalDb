namespace EfLocalDb;

public partial class SqlDatabase<TDbContext> :
#if(NET7_0_OR_GREATER)
    IServiceScopeFactory,
#endif
    IServiceProvider
{
    /// <summary>
    /// Gets a service of the specified type.
    /// Supports <see cref="SqlConnection"/> (returns <see cref="Connection"/>),
    /// <typeparamref name="TDbContext"/> (returns <see cref="Context"/>),
    /// and IServiceScopeFactory (returns this instance, .NET 7+ only).
    /// </summary>
    /// <param name="type">The type of service to get.</param>
    /// <returns>The service instance, or null if not supported.</returns>
    public object? GetService(Type type)
    {
        if (type == typeof(SqlConnection))
        {
            return Connection;
        }

        if (type == typeof(TDbContext))
        {
            return Context;
        }

#if(NET7_0_OR_GREATER)
        if (type == typeof(IServiceScopeFactory))
        {
            return this;
        }
#endif

        return null;
    }

#if(NET7_0_OR_GREATER)
    /// <summary>
    /// Creates a new <see cref="IServiceScope"/> with its own database connection and DbContext.
    /// The scope's connection and context are independent of <see cref="Connection"/> and <see cref="Context"/>.
    /// </summary>
    /// <returns>A new service scope.</returns>
    public IServiceScope CreateScope()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return new ServiceScope(NewDbContext(), connection);
    }

    /// <summary>
    /// Creates a new <see cref="AsyncServiceScope"/> with its own database connection and DbContext.
    /// </summary>
    /// <returns>A new async service scope.</returns>
    public AsyncServiceScope CreateAsyncScope() =>
        new(CreateScope());
#endif
}
