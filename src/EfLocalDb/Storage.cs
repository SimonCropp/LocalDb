namespace EfLocalDb;

public struct Storage
{
    public static Storage FromSuffix<TDbContext>(string suffix)
    {
        Guard.AgainstNullWhiteSpace(suffix);
        var instanceName = GetInstanceName<TDbContext>(suffix);
        return new(instanceName, DirectoryFinder.Find(instanceName));
    }

    public Storage(string name, string directory)
    {
        Guard.AgainstNullWhiteSpace(directory);
        Guard.AgainstNullWhiteSpace(name);
        Name = name;
        Directory = directory;
    }

    public string Directory { get; }

    public string Name { get; }

    static string GetInstanceName<TDbContext>(string? scopeSuffix)
    {
        Guard.AgainstWhiteSpace(scopeSuffix);

        #region GetInstanceName

        if (scopeSuffix is null)
        {
            return typeof(TDbContext).Name;
        }

        return $"{typeof(TDbContext).Name}_{scopeSuffix}";

        #endregion
    }
}