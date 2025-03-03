﻿[TestFixture]
public class EfSnippetTests
{
    static SqlInstance<MyDbContext> sqlInstance;

    static EfSnippetTests() =>
        sqlInstance = new(
            connection => new(connection),
            storage: Storage.FromSuffix<MyDbContext>("ClassicEfSnippetTests"));

    [Test]
    public async Task TheTest()
    {
        #region EfClassicBuildDatabase

        using var database = await sqlInstance.Build();

        #endregion

        #region EfClassicBuildContext

        using (var data = database.NewDbContext())
        {

            #endregion

            var entity = new TheEntity
            {
                Property = "prop"
            };
            data.TestEntities.Add(entity);
            await data.SaveChangesAsync();
        }

        using (var data = database.NewDbContext())
        {
            AreEqual(1, data.TestEntities.Count());
        }
    }

    [Test]
    public async Task TheTestWithDbName()
    {
        using var database = await sqlInstance.Build("TheTestWithDbName");
        var entity = new TheEntity
        {
            Property = "prop"
        };
        await database.AddData(entity);

        AreEqual(1, database.Context.TestEntities.Count());
    }
}