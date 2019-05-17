using LocalDb;

#region ModuleInitializer

static class ModuleInitializer
{
    public static void Initialize()
    {
        SqlInstanceService.Register(
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