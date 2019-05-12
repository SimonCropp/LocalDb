using EFLocalDb;

static class ModuleInitializer
{
    public static void Initialize()
    {
        LocalDb<TestDataContext>.Init(
            (connection, optionsBuilder) =>
            {
                using (var dataContext = new TestDataContext(optionsBuilder.Options))
                {
                    dataContext.Database.EnsureCreated();
                }
            },
            builder => new TestDataContext(builder.Options));
    }
}