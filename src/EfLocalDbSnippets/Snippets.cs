using EfLocalDb;

class Snippets
{
    Snippets()
    {
        #region EfRegisterExplcit

        SqlInstanceService<TheDbContext>.Register(
            buildTemplate: (connection, builder) =>
            {
                using (var dbContext = new TheDbContext(builder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            constructInstance: builder => new TheDbContext(builder.Options),
            instanceName: "theInstanceName",
            directory: @"C:\LocalDb\theInstance"
        );

        #endregion
    }
}