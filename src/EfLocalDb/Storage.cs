namespace EfLocalDb;

public struct Storage
{
    public static Storage FromSuffix<TDbContext>(string suffix)
    {
        Ensure.NotNullOrWhiteSpace(suffix);
        var instanceName = GetInstanceName<TDbContext>(suffix);
        return new(instanceName, DirectoryFinder.Find(instanceName));
    }

    public Storage(string name, string directory)
    {
        Ensure.NotNullOrWhiteSpace(directory);
        Ensure.NotNullOrWhiteSpace(name);
        Name = name;
        Directory = directory;
    }

    public string Directory { get; }

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
