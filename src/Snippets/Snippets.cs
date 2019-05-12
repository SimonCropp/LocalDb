using EFLocalDb;

class Snippets
{
    Snippets()
    {
        #region RegisterExplcit

        LocalDb<TheDbContext>.Register(
            (connection, optionsBuilder) =>
            {
                using (var dbContext = new TheDbContext(optionsBuilder.Options))
                {
                    dbContext.Database.EnsureCreated();
                }
            },
            builder => new TheDbContext(builder.Options),
            instanceName: "theInstanceName",
            directory: @"C:\EfLocalDb\theInstance"
        );

        #endregion
    }
}