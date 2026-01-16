namespace EfLocalDb;

/// <summary>
/// A delegate that constructs a <typeparamref name="TDbContext"/> instance from a configured options builder.
/// Used by <see cref="SqlInstance{TDbContext}"/> to create DbContext instances for database operations.
/// </summary>
/// <typeparam name="TDbContext">The Entity Framework Core DbContext type to construct.</typeparam>
/// <param name="optionsBuilder">
/// A <see cref="DbContextOptionsBuilder{TDbContext}"/> pre-configured with the SQL Server connection.
/// The delegate should call <c>optionsBuilder.Options</c> to get the configured options for the DbContext constructor.
/// </param>
/// <returns>A new instance of <typeparamref name="TDbContext"/>.</returns>
/// <example>
/// <code>
/// ConstructInstance&lt;MyDbContext&gt; construct = builder => new MyDbContext(builder.Options);
/// </code>
/// </example>
public delegate TDbContext ConstructInstance<TDbContext>(DbContextOptionsBuilder<TDbContext> optionsBuilder)
    where TDbContext : DbContext;