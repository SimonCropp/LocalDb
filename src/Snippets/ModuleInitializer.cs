using EFLocalDb;

#region ModuleInitializer

static class ModuleInitializer
{
    public static void Initialize()
    {
        LocalDb<MyDbContext>.Register(
            (connection, optionsBuilder) =>
            {
                using (var dbContext = new MyDbContext(optionsBuilder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            builder => new MyDbContext(builder.Options));
    }
}
#endregion