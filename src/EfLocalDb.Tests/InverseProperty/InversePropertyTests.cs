public class InversePropertyTests
{
    [Test]
    public async Task Run()
    {
        var sqlInstance = new SqlInstance<InversePropertyDbContext>(
            buildTemplate: CreateDb,
            constructInstance: builder => new(builder.Options));

        var database = await sqlInstance.Build("InverseProperty");
        var items = await database.Context.Employees.ToListAsync();
        IsNotEmpty(items);
    }

    static async Task CreateDb(InversePropertyDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        var employee1 = new Employee
        {
            Id = 2,
            Content = "Employee1",
            Age = 25
        };
        context.AddRange(employee1);

        await context.SaveChangesAsync();
    }
}