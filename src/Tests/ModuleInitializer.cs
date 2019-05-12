using EFLocalDb;

static class ModuleInitializer
{
    public static void Initialize()
    {
        LocalDb<ScopedDataContext>.Register(
            (connection, optionsBuilder) =>
            {
                using (var dataContext = new ScopedDataContext(optionsBuilder.Options))
                {
                    dataContext.Database.EnsureCreated();
                }
            },
            builder => new ScopedDataContext(builder.Options),
            scopeSuffix:"theSuffix");
        LocalDb<TestDataContext>.Register(
            (connection, optionsBuilder) =>
            {
                using (var dataContext = new TestDataContext(optionsBuilder.Options))
                {
                    dataContext.Database.EnsureCreated();
                }
            },
            builder => new TestDataContext(builder.Options));
        LocalDb<SecondaryDataContext>.Register(
            (connection, optionsBuilder) =>
            {
                using (var dataContext = new SecondaryDataContext(optionsBuilder.Options))
                {
                    dataContext.Database.EnsureCreated();
                }
            },
            builder => new SecondaryDataContext(builder.Options));
    }
}