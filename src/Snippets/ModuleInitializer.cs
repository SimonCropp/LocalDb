using EFLocalDb;

namespace Snippet
{
    #region ModuleInitializer

    static class ModuleInitializer
    {
        public static void Initialize()
        {
            LocalDb<TheDbContext>.Register(
                (connection, optionsBuilder) =>
                {
                    using (var dbContext = new TheDbContext(optionsBuilder.Options))
                    {
                        dbContext.Database.EnsureCreated();
                    }
                },
                builder => new TheDbContext(builder.Options));
        }
    }
    #endregion
}