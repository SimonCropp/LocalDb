static class ModuleInitializer
{
    public static void Initialize()
    {
        LocalDB<TestDataContext>.Init(
            "Foo",
            (connection, optionsBuilder) =>
            {
                //TODO:
                //optionsBuilder.ReplaceService<IMigrationsSqlGenerator, CustomMigrationsSqlGenerator>();
                using (var dataContext = new TestDataContext(optionsBuilder.Options))
                {
                    dataContext.Database.EnsureCreated();
                    //TODO:
                    //dataContext.Database.Migrate();
                }

                //TODO:
                // TrackChanges.EnableChangeTrackingOnDb(connection);
            },
            builder => new TestDataContext(builder.Options));
    }
}