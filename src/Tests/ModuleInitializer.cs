using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class ModuleInitializer
{
    public static void Initialize()
    {
        LocalDB<TestDataContext>.Init("Foo", connection =>
        {
                var builder = new DbContextOptionsBuilder<TestDataContext>();
                builder.ConfigureWarnings(warnings => warnings.Throw(CoreEventId.IncludeIgnoredWarning));
                builder.UseSqlServer(connection);
                //TODO:
                //optionsBuilder.ReplaceService<IMigrationsSqlGenerator, CustomMigrationsSqlGenerator>();
                using (var dataContext = new TestDataContext(builder.Options))
                {
                    dataContext.Database.EnsureCreated();
                    //TODO:
                    //dataContext.Database.Migrate();
                }
                //TODO:
                // TrackChanges.EnableChangeTrackingOnDb(connection);
        });
    }
}