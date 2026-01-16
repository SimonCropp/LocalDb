namespace EfLocalDb;

/// <summary>
/// A delegate that constructs a <typeparamref name="TDbContext"/> instance from a SQL connection.
/// Used by <see cref="SqlInstance{TDbContext}"/> to create DbContext instances for database operations.
/// </summary>
/// <typeparam name="TDbContext">The Entity Framework 6 (Classic) DbContext type to construct.</typeparam>
/// <param name="connection">
/// An open <see cref="SqlConnection"/> to use for the DbContext.
/// The delegate should pass this connection to the DbContext constructor, typically with <c>contextOwnsConnection: false</c>.
/// </param>
/// <returns>A new instance of <typeparamref name="TDbContext"/>.</returns>
/// <example>
/// <code>
/// ConstructInstance&lt;MyDbContext&gt; construct = connection => new MyDbContext(connection, contextOwnsConnection: false);
/// </code>
/// </example>
public delegate TDbContext ConstructInstance<out TDbContext>(SqlConnection connection)
    where TDbContext : DbContext;