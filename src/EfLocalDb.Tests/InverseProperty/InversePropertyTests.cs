[TestFixture]
public class InversePropertyTests
{
    [Test]
    public async Task Run()
    {
        using var instance = new SqlInstance<InversePropertyDbContext>(
            buildTemplate: CreateDb,
            constructInstance: builder => new(builder.Options));

        var database = await instance.Build(dbName: "InverseProperty");
        var items = await database.Context.Employees.ToListAsync();
        IsNotEmpty(items);
        instance.Cleanup();
    }

    static async Task CreateDb(InversePropertyDbContext context, Cancel cancel = default)
    {
        await context.Database.EnsureCreatedAsync(cancel);

        var employee1 = new Employee
        {
            Id = 2,
            Content = "Employee1",
            Age = 25
        };
        context.AddRange(employee1);

        await context.SaveChangesAsync(cancel);
    }
}
