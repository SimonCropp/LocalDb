namespace EfLocalDb
{
    public struct Name<TDbContext>
    {
        public static Name<TDbContext> FromSuffix(string suffix)
        {
            Guard.AgainstWhiteSpace(nameof(suffix), suffix);
            var instanceName = GetInstanceName(suffix);
            return new Name<TDbContext>(instanceName,DirectoryFinder.Find(instanceName));
        }

        public Name(string instanceName, string directory)
        {
            Guard.AgainstNullWhiteSpace(nameof(directory), directory);
            Guard.AgainstNullWhiteSpace(nameof(instanceName), instanceName);
            InstanceName = instanceName;
            Directory = directory;
        }

        public string Directory { get; private set; }

        public string InstanceName { get; private set; }

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

        public static Name<TDbContext> Default;

        static Name()
        {
            Default = new Name<TDbContext>();

            var instanceName = typeof(TDbContext).Name;
            Default.InstanceName = instanceName;
            Default.Directory = DirectoryFinder.Find(instanceName);
        }
    }
}