namespace EfLocalDb
{
    public struct Storage
    {
        public static Storage FromSuffix<TDbContext>(string suffix)
        {
            Guard.AgainstNullWhiteSpace(nameof(suffix), suffix);
            var instanceName = GetInstanceName<TDbContext>(suffix);
            return new(instanceName, DirectoryFinder.Find(instanceName));
        }

        public Storage(string name, string directory)
        {
            Guard.AgainstNullWhiteSpace(nameof(directory), directory);
            Guard.AgainstNullWhiteSpace(nameof(name), name);
            Name = name;
            Directory = directory;
        }

        public string Directory { get; }

        public string Name { get; }

        static string GetInstanceName<TDbContext>(string? scopeSuffix)
        {
            Guard.AgainstWhiteSpace(nameof(scopeSuffix), scopeSuffix);

            #region GetInstanceName

            if (scopeSuffix is null)
            {
                return typeof(TDbContext).Name;
            }

            return $"{typeof(TDbContext).Name}_{scopeSuffix}";

            #endregion
        }
    }
}