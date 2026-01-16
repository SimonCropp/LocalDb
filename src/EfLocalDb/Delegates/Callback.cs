namespace EfLocalDb;

/// <summary>
/// A delegate that is executed after the template database has been created or mounted.
/// Use this for post-creation setup such as seeding reference data or configuring database settings.
/// Guaranteed to be called exactly once per <see cref="SqlInstance{TDbContext}"/> lifetime.
/// </summary>
/// <typeparam name="TDbContext">The Entity Framework Core DbContext type.</typeparam>
/// <param name="connection">
/// An open <see cref="SqlConnection"/> to the template database.
/// Use for direct SQL commands if needed.
/// </param>
/// <param name="context">
/// A <typeparamref name="TDbContext"/> instance connected to the template database.
/// Use for Entity Framework operations like seeding data.
/// </param>
/// <returns>A <see cref="Task"/> representing the asynchronous callback operation.</returns>
/// <example>
/// <code>
/// Callback&lt;MyDbContext&gt; callback = async (connection, context) =>
/// {
///     // Seed reference data that should exist in all test databases
///     context.Users.Add(new User { Id = 1, Name = "System" });
///     await context.SaveChangesAsync();
///
///     // Or run direct SQL
///     await using var cmd = connection.CreateCommand();
///     cmd.CommandText = "INSERT INTO Settings (Key, Value) VALUES ('Version', '1.0')";
///     await cmd.ExecuteNonQueryAsync();
/// };
/// </code>
/// </example>
public delegate Task Callback<in TDbContext>(SqlConnection connection, TDbContext context)
    where TDbContext : DbContext;