namespace EfLocalDb;

/// <summary>
/// A delegate that builds the template database schema using direct SQL connection access and a DbContext options builder.
/// The template is then cloned for each test database.
/// Called zero or once based on the current state of the underlying LocalDB:
/// not called if a valid template already exists, called once if the template needs to be created or rebuilt.
/// Use this overload when you need low-level SQL access for schema creation, such as running migration scripts.
/// </summary>
/// <typeparam name="TDbContext">The Entity Framework Core DbContext type.</typeparam>
/// <param name="connection">
/// An open <see cref="SqlConnection"/> to the template database.
/// Use for direct SQL commands or pass to a DbContext via the options builder.
/// </param>
/// <param name="optionsBuilder">
/// A <see cref="DbContextOptionsBuilder{TDbContext}"/> pre-configured to use the provided connection.
/// Use <c>optionsBuilder.Options</c> to construct a DbContext if needed.
/// </param>
/// <returns>A <see cref="Task"/> representing the asynchronous template building operation.</returns>
/// <example>
/// <code>
/// TemplateFromConnection&lt;MyDbContext&gt; buildTemplate = async (connection, builder) =>
/// {
///     // Option 1: Use DbContext
///     await using var context = new MyDbContext(builder.Options);
///     await context.Database.MigrateAsync();
///
///     // Option 2: Use direct SQL
///     await using var cmd = connection.CreateCommand();
///     cmd.CommandText = "CREATE TABLE ...";
///     await cmd.ExecuteNonQueryAsync();
/// };
/// </code>
/// </example>
public delegate Task TemplateFromConnection<TDbContext>(SqlConnection connection, DbContextOptionsBuilder<TDbContext> optionsBuilder)
    where TDbContext : DbContext;