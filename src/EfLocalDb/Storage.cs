namespace EfLocalDb;

/// <summary>
/// Represents the disk storage configuration for a <see cref="SqlInstance{TDbContext}"/>.
/// Specifies where the .mdf (data) and .ldf (log) database files will be stored.
/// </summary>
/// <example>
/// <code>
/// // Use default storage (derived from DbContext name)
/// var instance = new SqlInstance&lt;MyDbContext&gt;(builder => new MyDbContext(builder.Options));
///
/// // Use custom storage with explicit name and directory
/// var storage = new Storage("MyCustomInstance", @"C:\Databases\MyApp");
/// var instance = new SqlInstance&lt;MyDbContext&gt;(builder => new MyDbContext(builder.Options), storage: storage);
///
/// // Use suffixed storage for parallel test runs
/// var storage = Storage.FromSuffix&lt;MyDbContext&gt;("Worker1");
/// var instance = new SqlInstance&lt;MyDbContext&gt;(builder => new MyDbContext(builder.Options), storage: storage);
/// </code>
/// </example>
public struct Storage
{
    /// <summary>
    /// Creates a <see cref="Storage"/> instance with a name derived from <typeparamref name="TDbContext"/> and the specified suffix.
    /// Useful for creating isolated instances for parallel test execution.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type used to derive the base instance name.</typeparam>
    /// <param name="suffix">
    /// A suffix to append to the DbContext name to create a unique instance name.
    /// For example, a suffix of "Worker1" with a DbContext named "MyDbContext" produces "MyDbContext_Worker1".
    /// </param>
    /// <returns>A new <see cref="Storage"/> instance with the suffixed name and an auto-discovered directory.</returns>
    /// <example>
    /// <code>
    /// // Creates storage named "MyDbContext_Worker1"
    /// var storage = Storage.FromSuffix&lt;MyDbContext&gt;("Worker1");
    /// </code>
    /// </example>
    public static Storage FromSuffix<TDbContext>(string suffix)
    {
        Ensure.NotNullOrWhiteSpace(suffix);
        var instanceName = GetInstanceName<TDbContext>(suffix);
        return new(instanceName, DirectoryFinder.Find(instanceName));
    }

    /// <summary>
    /// Initializes a new <see cref="Storage"/> instance with the specified name and directory.
    /// </summary>
    /// <param name="name">
    /// The name of the LocalDB instance. Must be unique across all SqlInstance instances running concurrently.
    /// Used to identify the instance in SQL Server LocalDB.
    /// </param>
    /// <param name="directory">
    /// The directory path where the .mdf and .ldf database files will be stored.
    /// The directory will be created if it does not exist.
    /// </param>
    /// <example>
    /// <code>
    /// var storage = new Storage("MyTestInstance", @"C:\TestDatabases\MyApp");
    /// </code>
    /// </example>
    public Storage(string name, string directory)
    {
        Ensure.NotNullOrWhiteSpace(directory);
        Ensure.NotNullOrWhiteSpace(name);
        Name = name;
        Directory = directory;
    }

    /// <summary>
    /// Gets the directory path where database files (.mdf and .ldf) are stored.
    /// </summary>
    public string Directory { get; }

    /// <summary>
    /// Gets the name of the LocalDB instance.
    /// </summary>
    public string Name { get; }

    static string GetInstanceName<TDbContext>(string? scopeSuffix)
    {
        Ensure.NotWhiteSpace(scopeSuffix);

        #region GetInstanceName

        if (scopeSuffix is null)
        {
            return typeof(TDbContext).Name;
        }

        return $"{typeof(TDbContext).Name}_{scopeSuffix}";

        #endregion
    }
}
