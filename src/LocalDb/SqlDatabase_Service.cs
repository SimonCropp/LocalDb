namespace LocalDb;

public partial class SqlDatabase :
#if NET7_0_OR_GREATER
    IServiceScopeFactory,
#endif
    IServiceProvider
{
    /// <summary>
    /// Gets a service of the specified type.
    /// Supports <see cref="SqlConnection"/> (returns <see cref="Connection"/>)
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

#if NET7_0_OR_GREATER
        if (type == typeof(IServiceScopeFactory))
        {
            return this;
        }
#endif

        return null;
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Creates a new <see cref="IServiceScope"/> with its own database connection.
    /// The scope's connection is independent of <see cref="Connection"/>.
    /// </summary>
    /// <returns>A new service scope.</returns>
    public IServiceScope CreateScope()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return new ServiceScope(connection);
    }

    /// <summary>
    /// Creates a new <see cref="AsyncServiceScope"/> with its own database connection.
    /// </summary>
    /// <returns>A new async service scope.</returns>
    public AsyncServiceScope CreateAsyncScope() =>
        new(CreateScope());
#endif
}
