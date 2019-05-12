using EFLocalDb;

static class ModuleInitializer
{
    public static void Initialize()
    {
        LocalDb<ScopedDbContext>.Register(
            (connection, optionsBuilder) =>
            {
                using (var dbContext = new ScopedDbContext(optionsBuilder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            builder => new ScopedDbContext(builder.Options),
            scopeSuffix: "theSuffix");
        LocalDb<TestDbContext>.Register(
            (connection, optionsBuilder) =>
            {
                using (var dbContext = new TestDbContext(optionsBuilder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            builder => new TestDbContext(builder.Options));
        LocalDb<SecondaryDbContext>.Register(
            (connection, optionsBuilder) =>
            {
                using (var dbContext = new SecondaryDbContext(optionsBuilder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            builder => new SecondaryDbContext(builder.Options));
    }
}