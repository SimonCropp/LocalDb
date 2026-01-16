namespace EfLocalDb;

/// <summary>
/// A delegate that builds the template database schema using direct SQL connection access.
/// Called once when the template is first created. The template is then cloned for each test database.
/// Use this overload when you need low-level SQL access for schema creation, such as running migration scripts.
/// </summary>
/// <param name="connection">
/// An open <see cref="SqlConnection"/> to the template database.
/// Use for direct SQL commands to create tables, indexes, stored procedures, etc.
/// </param>
/// <returns>A <see cref="Task"/> representing the asynchronous template building operation.</returns>
/// <example>
/// <code>
/// TemplateFromConnection buildTemplate = async connection =>
/// {
///     await using var cmd = connection.CreateCommand();
///     cmd.CommandText = @"
///         CREATE TABLE Users (
///             Id INT PRIMARY KEY IDENTITY,
///             Name NVARCHAR(100) NOT NULL
///         )";
///     await cmd.ExecuteNonQueryAsync();
/// };
/// </code>
/// </example>
public delegate Task TemplateFromConnection(SqlConnection connection);