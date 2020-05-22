namespace EfLocalDb
{
    public struct Storage<TDbContext>
    {
        public static Storage<TDbContext> FromSuffix(string suffix)
        {
            Guard.AgainstWhiteSpace(nameof(suffix), suffix);
            var instanceName = GetInstanceName(suffix);
            return new Storage<TDbContext>(instanceName,DirectoryFinder.Find(instanceName));
        }

        public Storage(string name, string directory)
        {
            Guard.AgainstNullWhiteSpace(nameof(directory), directory);
            Guard.AgainstNullWhiteSpace(nameof(name), name);
            Name = name;
            Directory = directory;
        }

        public string Directory { get; private set; }

        public string Name { get; private set; }

        static string GetInstanceName(string? scopeSuffix)
        {
            Guard.AgainstWhiteSpace(nameof(scopeSuffix), scopeSuffix);

            #region GetInstanceName

            if (scopeSuffix == null)
            {
                return typeof(TDbContext).Name;
            }

            return $"{typeof(TDbContext).Name}_{scopeSuffix}";

            #endregion
        }

        public static Storage<TDbContext> Default;

        static Storage()
        {
            Default = new Storage<TDbContext>();

            var instanceName = typeof(TDbContext).Name;
            Default.Name = instanceName;
            Default.Directory = DirectoryFinder.Find(instanceName);
        }
    }
}