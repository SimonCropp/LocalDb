using LocalDb;

class Snippets
{
    Snippets()
    {
        #region RegisterExplcit

        SqlInstanceService.Register(
            buildTemplate: (connection, builder) =>
            {
                using (var dbContext = new TheDbContext(builder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            constructInstance: builder => new TheDbContext(builder.Options),
            instanceName: "theInstanceName",
            directory: @"C:\EfLocalDb\theInstance"
        );

        #endregion
    }
}