[TestClass]
public class Tests :
    LocalDbTestBase<TheDbContext>
{
    [TestMethod]
    public async Task Simple()
    {
        ArrangeData.Companies.Add(
            new()
            {
                Id = Guid.NewGuid(),
                Name = "value"
            });
        await ArrangeData.SaveChangesAsync();

        var entity = await ActData.Companies.SingleAsync();
        entity.Name = "value2";
        await ActData.SaveChangesAsync();

        var result = await AssertData.Companies.SingleAsync();
        await Verify(result);
    }

    // begin-snippet: StaticInstanceMSTest
    [TestMethod]
    public async Task StaticInstance()
    {
        Instance.ArrangeData.Companies.Add(
            new()
            {
                Id = Guid.NewGuid(),
                Name = "value"
            });
        await Instance.ArrangeData.SaveChangesAsync();

        var entity = await Instance.ActData.Companies.SingleAsync();
        entity.Name = "value2";
        await Instance.ActData.SaveChangesAsync();

        var result = await Instance.AssertData.Companies.SingleAsync();
        await Verify(result);
    }
    // end-snippet

    // begin-snippet: CombinationsMSTest
    [TestMethod]
    public Task Combinations()
    {
        string[] inputs = ["value1", "value2"];
        return Combination()
            .Verify(Run, inputs);

        async Task<Company> Run(string input)
        {
            ArrangeData.Companies.Add(
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "value"
                });
            await ArrangeData.SaveChangesAsync();

            var entity = await ActData.Companies.SingleAsync();
            entity.Name = input;
            await ActData.SaveChangesAsync();

            return await AssertData.Companies.SingleAsync();
        }
    }
    // end-snippet

    [TestMethod]
    public Task Name() =>
        Verify(new
        {
            Database.Name,
            Database.Connection.DataSource
        });

    [TestMethod]
    public Task ThrowForRedundantIgnoreQueryFilters() =>
        ThrowsTask(
                () =>
                {
                    var entities = AssertData.Companies;
                    return entities.IgnoreQueryFilters().SingleAsync();
                })
            .IgnoreStackTrace();

    [TestMethod]
    public async Task IgnoreQueryFiltersAllowedOnArrangeAndAct()
    {
        await ArrangeData.Companies.IgnoreQueryFilters().ToListAsync();
        await ActData.Companies.IgnoreQueryFilters().ToListAsync();
    }

    [TestMethod]
    public async Task ActInAsync()
    {
        ArrangeData.Companies.Add(
            new()
            {
                Id = Guid.NewGuid(),
                Name = "value"
            });
        await ArrangeData.SaveChangesAsync();

        await AsyncMethod();

        var result = await AssertData.Companies.SingleAsync();
        await Verify(result);

        async Task AsyncMethod()
        {
            await Task.Delay(1);
            var entity = await ActData.Companies.SingleAsync();
            entity.Name = "value2";
            await ActData.SaveChangesAsync();
        }
    }

    // begin-snippet: VerifyEntity
    [TestMethod]
    public async Task VerifyEntityById()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "value"
        };
        ArrangeData.Companies.Add(company);
        await ArrangeData.SaveChangesAsync();
        await VerifyEntity<Company>(company.Id);
    }
    // end-snippet

    [TestMethod]
    public async Task VerifyEntityByIdNull() =>
        await VerifyEntity<Company>(Guid.NewGuid());

    // begin-snippet: VerifyEntityWithInclude
    [TestMethod]
    public async Task VerifyEntityWithInclude()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "the Company"
        };
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Name = "the Employee"
        };
        ArrangeData.AddRange(company, employee);
        await ArrangeData.SaveChangesAsync();
        await VerifyEntity<Company>(company.Id)
            .Include(_ => _.Employees);
    }
    // end-snippet

    // begin-snippet: VerifyEntityWithThenInclude
    [TestMethod]
    public async Task VerifyEntityWithThenInclude()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "the Company"
        };
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Name = "the Employee"
        };
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            Model = "the Vehicle"
        };
        ArrangeData.AddRange(company, employee, vehicle);
        await ArrangeData.SaveChangesAsync();
        await VerifyEntity<Company>(company.Id)
            .Include(_ => _.Employees)
            .ThenInclude(_ => _.Vehicles);
    }
    // end-snippet

    // begin-snippet: VerifyEntities_DbSet
    [TestMethod]
    public async Task VerifyEntities_DbSet()
    {
        ArrangeData.Companies.Add(
            new()
            {
                Id = Guid.NewGuid(),
                Name = "value"
            });
        await ArrangeData.SaveChangesAsync();
        await VerifyEntities(AssertData.Companies);
    }
    // end-snippet

    [TestMethod]
    public async Task VerifyEntities_Queryable()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "value"
        };
        ArrangeData.Companies.Add(company);
        await ArrangeData.SaveChangesAsync();
        await VerifyEntities(AssertData.Companies.Where(_ => _.Id == company.Id));
    }

    // begin-snippet: VerifyEntity_Queryable
    [TestMethod]
    public async Task VerifyEntity_Queryable()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "value"
        };
        ArrangeData.Companies.Add(company);
        await ArrangeData.SaveChangesAsync();
        await VerifyEntity(AssertData.Companies.Where(_ => _.Id == company.Id));
    }
    // end-snippet

    [TestMethod]
    public async Task ArrangeQueryableAfterAct()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "value"
        };
        ArrangeData.Companies.Add(company);
        await ArrangeData.SaveChangesAsync();
        var queryable = ArrangeData.Companies.Where(_ => _.Id == company.Id);
        // ReSharper disable once UnusedVariable
        var act = ActData;
        await ThrowsTask(() => VerifyEntities(queryable))
            .IgnoreStackTrace()
            .DisableRequireUniquePrefix();
    }

    [TestMethod]
    public Task AccessActAfterAssert()
    {
        // ReSharper disable once UnusedVariable
        var assert = AssertData;
        return Throws(() => ActData)
            .IgnoreStackTrace();
    }

    [TestMethod]
    public async Task ActQueryableAfterAssert()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "value"
        };
        ArrangeData.Companies.Add(company);
        await ArrangeData.SaveChangesAsync();
        var queryable = ActData.Companies.Where(_ => _.Id == company.Id);
        // ReSharper disable once UnusedVariable
        var assert = AssertData;
        await ThrowsTask(() => VerifyEntities(queryable))
            .IgnoreStackTrace()
            .DisableRequireUniquePrefix();
    }

    [TestMethod]
    public async Task ArrangeQueryableAfterAssert()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "value"
        };
        ArrangeData.Companies.Add(company);
        await ArrangeData.SaveChangesAsync();
        var queryable = ArrangeData.Companies.Where(_ => _.Id == company.Id);
        // ReSharper disable once UnusedVariable
        var assert = AssertData;
        await ThrowsTask(() => VerifyEntities(queryable))
            .IgnoreStackTrace()
            .DisableRequireUniquePrefix();
    }

    [TestMethod]
    public Task AccessArrangeAfterAssert()
    {
        // ReSharper disable once UnusedVariable
        var assert = AssertData;
        return Throws(() => ArrangeData)
            .IgnoreStackTrace();
    }

    [TestMethod]
    public Task AccessArrangeAfterAct()
    {
        // ReSharper disable once UnusedVariable
        var act = ActData;
        return Throws(() => ArrangeData)
            .IgnoreStackTrace();
    }
}
