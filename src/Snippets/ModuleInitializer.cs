using EFLocalDb;

#region ModuleInitializer

static class ModuleInitializer
{
    public static void Initialize()
    {
        LocalDb<MyDbContext>.Register(
            buildTemplate: (connection, builder) =>
            {
                using (var dbContext = new MyDbContext(builder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            constructInstance: builder => new MyDbContext(builder.Options));
    }
}
#endregion