[TestFixture]
public class Tests :
    LocalDbTestBase<TheDbContext>
{
    #region NUnitSimple

    [Test]
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

    #endregion

    #region NUnitStaticInstance

    [Test]
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

    #endregion

    #region NUnitCombinations

    [Test]
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

    #endregion

    [Test]
    public Task Name() =>
        Verify(new
        {
            Database.Name,
            Database.Connection.DataSource
        });

    [Test]
    [TestCase("case")]
    public Task NameWithParams(string caseName) =>
        Verify(new
        {
            Database.Name,
            Database.Connection.DataSource
        });

    [Test]
    public Task ThrowForRedundantIgnoreQueryFilters() =>
        ThrowsTask(
                () =>
                {
                    var entities = AssertData.Companies;
                    return entities.IgnoreQueryFilters().SingleAsync();
                })
            .IgnoreStackTrace();

    [Test]
    public async Task IgnoreQueryFiltersAllowedOnArrangeAndAct()
    {
        await ArrangeData.Companies.IgnoreQueryFilters().ToListAsync();
        await ActData.Companies.IgnoreQueryFilters().ToListAsync();
    }

    [Test]
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

    [Test]
    public async Task VerifyEntity()
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

    [Test]
    public async Task VerifyEntityNull() =>
        await VerifyEntity<Company>(Guid.NewGuid());

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
    public Task AccessActAfterAssert()
    {
        // ReSharper disable once UnusedVariable
        var assert = AssertData;
        return Throws(() => ActData)
            .IgnoreStackTrace();
    }

    [Test]
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

    [Test]
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

    [Test]
    public Task AccessArrangeAfterAssert()
    {
        // ReSharper disable once UnusedVariable
        var assert = AssertData;
        return Throws(() => ArrangeData)
            .IgnoreStackTrace();
    }

    [Test]
    public Task AccessArrangeAfterAct()
    {
        // ReSharper disable once UnusedVariable
        var act = ActData;
        return Throws(() => ArrangeData)
            .IgnoreStackTrace();
    }
}