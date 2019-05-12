using System.Threading.Tasks;
using EFLocalDb;
using Xunit;

public class Tests
{
    [Fact]
    public async Task Simple()
    {
        var localDb = await LocalDb<TestDataContext>.Build(this);
        using (var dataContext = localDb.NewDataContext())
        {
            var entity = new TestEntity
            {
                Property = "prop"
            };
            dataContext.Add(entity);
            dataContext.SaveChanges();
        }
    }
}