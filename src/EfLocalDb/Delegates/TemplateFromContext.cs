namespace EfLocalDb;

/// <summary>
/// A delegate that builds the template database schema using a <typeparamref name="TDbContext"/> instance.
/// Called once when the template is first created. The template is then cloned for each test database.
/// </summary>
/// <typeparam name="TDbContext">The Entity Framework Core DbContext type.</typeparam>
/// <param name="context">
/// A <typeparamref name="TDbContext"/> instance connected to the template database.
/// Use this to create schema (e.g., <c>context.Database.EnsureCreatedAsync()</c> or <c>context.Database.MigrateAsync()</c>)
/// and optionally seed initial reference data.
/// </param>
/// <returns>A <see cref="Task"/> representing the asynchronous template building operation.</returns>
/// <example>
/// <code>
/// TemplateFromContext&lt;MyDbContext&gt; buildTemplate = async context =>
/// {
///     await context.Database.EnsureCreatedAsync();
///     context.ReferenceData.Add(new ReferenceItem { Id = 1, Name = "Default" });
///     await context.SaveChangesAsync();
/// };
/// </code>
/// </example>
public delegate Task TemplateFromContext<in TDbContext>(TDbContext context)
    where TDbContext : DbContext;