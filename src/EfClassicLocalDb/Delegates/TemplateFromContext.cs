namespace EfLocalDb;

/// <summary>
/// A delegate that builds the template database schema using a <typeparamref name="TDbContext"/> instance.
/// The template is then cloned for each test database.
/// Called zero or once based on the current state of the underlying LocalDB:
/// not called if a valid template already exists, called once if the template needs to be created or rebuilt.
/// </summary>
/// <typeparam name="TDbContext">The Entity Framework 6 (Classic) DbContext type.</typeparam>
/// <param name="context">
/// A <typeparamref name="TDbContext"/> instance connected to the template database.
/// Use this to create schema (e.g., <c>context.Database.CreateIfNotExists()</c> or run migrations)
/// and optionally seed initial reference data.
/// </param>
/// <returns>A <see cref="Task"/> representing the asynchronous template building operation.</returns>
/// <example>
/// <code>
/// TemplateFromContext&lt;MyDbContext&gt; buildTemplate = async context =>
/// {
///     context.Database.CreateIfNotExists();
///     context.ReferenceData.Add(new ReferenceItem { Id = 1, Name = "Default" });
///     await context.SaveChangesAsync();
/// };
/// </code>
/// </example>
public delegate Task TemplateFromContext<in TDbContext>(TDbContext context)
    where TDbContext : DbContext;