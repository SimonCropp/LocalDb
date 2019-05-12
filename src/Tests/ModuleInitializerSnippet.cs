using EFLocalDb;

namespace Snippet
{
    #region ModuleInitializer

    static class ModuleInitializer
    {
        public static void Initialize()
        {
            LocalDb<TheDataContext>.Register(
                (connection, optionsBuilder) =>
                {
                    using (var dataContext = new TheDataContext(optionsBuilder.Options))
                    {
                        dataContext.Database.EnsureCreated();
                    }
                },
                builder => new TheDataContext(builder.Options));
        }
    }
    #endregion
}