[TestFixture]
public class TemporalTests
{
    static SqlInstance<TemporalDbContext> instance = new(builder => new(builder.Options));

    [OneTimeTearDown]
    public void Cleanup()
    {
        instance.Cleanup();
        instance.Dispose();
    }

    [Test]
    public async Task Backdates_CurrentRow()
    {
        await using var database = await instance.Build();
        var entity = new TemporalEntity { Id = Guid.NewGuid(), Property = "v1" };
        database.Context.Add(entity);
        await database.Context.SaveChangesAsync();

        var anchor = DateTime.UtcNow.AddSeconds(-30);
        await database.SetCurrentPeriodStart<TemporalEntity>(entity.Id, anchor);

        var actual = await database.Context.Set<TemporalEntity>()
            .Where(_ => _.Id == entity.Id)
            .Select(_ => EF.Property<DateTime>(_, "PeriodStart"))
            .SingleAsync();
        AreEqual(anchor, actual);
    }

    [Test]
    public async Task Aligns_HistoryEnd_AfterUpdate()
    {
        await using var database = await instance.Build();
        var entity = new TemporalEntity { Id = Guid.NewGuid(), Property = "v1" };
        database.Context.Add(entity);
        await database.Context.SaveChangesAsync();

        var anchor1 = DateTime.UtcNow.AddSeconds(-30);
        await database.SetCurrentPeriodStart(entity, anchor1);

        entity.Property = "v2";
        await database.Context.SaveChangesAsync();

        var anchor2 = anchor1.AddSeconds(1);
        await database.SetCurrentPeriodStart(entity, anchor2);

        var maxPeriod = new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
        var historyEnd = await database.Context.Set<TemporalEntity>()
            .TemporalAll()
            .Where(_ => _.Id == entity.Id && EF.Property<DateTime>(_, "PeriodEnd") < maxPeriod)
            .Select(_ => EF.Property<DateTime>(_, "PeriodEnd"))
            .SingleAsync();
        AreEqual(anchor2, historyEnd);

        var currentStart = await database.Context.Set<TemporalEntity>()
            .Where(_ => _.Id == entity.Id)
            .Select(_ => EF.Property<DateTime>(_, "PeriodStart"))
            .SingleAsync();
        AreEqual(anchor2, currentStart);
    }

    [Test]
    public async Task EntityOverload_ReloadsRowVersion()
    {
        await using var database = await instance.Build();
        var entity = new TemporalEntity { Id = Guid.NewGuid(), Property = "v1" };
        database.Context.Add(entity);
        await database.Context.SaveChangesAsync();

        // Helper's UPDATE bumps RowVersion. Entity overload reloads, so the next
        // SaveChanges must succeed without a concurrency exception.
        var anchor = DateTime.UtcNow.AddSeconds(-10);
        await database.SetCurrentPeriodStart(entity, anchor);

        entity.Property = "v2";
        await database.Context.SaveChangesAsync();
    }

    [Test]
    public async Task SetHistoryColumn_LeavesCurrentRowUntouched()
    {
        await using var database = await instance.Build();
        var entity = new TemporalEntity { Id = Guid.NewGuid(), Property = "v1" };
        database.Context.Add(entity);
        await database.Context.SaveChangesAsync();

        var anchor = DateTime.UtcNow.AddSeconds(-30);
        await database.SetCurrentPeriodStart(entity, anchor);

        entity.Property = "v2";
        await database.Context.SaveChangesAsync();
        await database.SetCurrentPeriodStart(entity, anchor.AddSeconds(1));

        await database.SetHistoryColumn<TemporalEntity>(entity.Id, nameof(TemporalEntity.Property), null);

        // The history row now holds a value the model says is impossible, which is the point:
        // this is what a column dropped and re-added on a temporal pair leaves behind.
        var maxPeriod = new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
        var history = await database.Context.Set<TemporalEntity>()
            .TemporalAll()
            .Where(_ => _.Id == entity.Id && EF.Property<DateTime>(_, "PeriodEnd") < maxPeriod)
            .Select(_ => _.Property)
            .SingleAsync();
        Null(history);

        var current = await database.Context.Set<TemporalEntity>()
            .Where(_ => _.Id == entity.Id)
            .Select(_ => _.Property)
            .SingleAsync();
        AreEqual("v2", current);
    }

    [Test]
    public async Task SetHistoryColumn_LeavesVersioningOn()
    {
        await using var database = await instance.Build();
        var entity = new TemporalEntity { Id = Guid.NewGuid(), Property = "v1" };
        database.Context.Add(entity);
        await database.Context.SaveChangesAsync();

        await database.SetHistoryColumn<TemporalEntity>(entity.Id, nameof(TemporalEntity.Property), null);

        // Versioning has to be off to write to a history table, so a helper that failed to turn
        // it back on would silently stop every later save in the test from being versioned.
        // The saves need distinct periods or SQL Server drops the zero-length history row, and
        // the count below then fails for a reason that has nothing to do with versioning.
        await database.SetCurrentPeriodStart(entity, DateTime.UtcNow.AddSeconds(-10));

        entity.Property = "v2";
        await database.Context.SaveChangesAsync();

        var versions = await database.Context.Set<TemporalEntity>()
            .TemporalAll()
            .CountAsync(_ => _.Id == entity.Id);
        AreEqual(2, versions);
    }

    [Test]
    public async Task SetHistoryColumn_ThrowsForPeriodColumn()
    {
        await using var database = await instance.Build();
        var ex = ThrowsAsync<InvalidOperationException>(() =>
            database.SetHistoryColumn<TemporalEntity>(Guid.NewGuid(), "PeriodEnd", DateTime.UtcNow));
        That(ex!.Message, Does.Contain("not a settable history column"));
    }

    [Test]
    public async Task Throws_WhenEntityNotTemporal()
    {
        await using var database = await instance.Build();
        var ex = ThrowsAsync<InvalidOperationException>(() =>
            database.SetCurrentPeriodStart<NonTemporalEntity>(Guid.NewGuid(), DateTime.UtcNow));
        That(ex!.Message, Does.Contain("not configured as a temporal table"));
    }
}
