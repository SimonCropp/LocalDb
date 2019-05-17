using EFLocalDb;

#region ModuleInitializer

static class ModuleInitializer
{
    public static void Initialize()
    {
        SqlInstanceService<MyDbContext>.Register(
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