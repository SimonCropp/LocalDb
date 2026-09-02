using System.ComponentModel.DataAnnotations;

public class TemporalSnippetTests
{
    #region TemporalEntityConfig

    public class TravelRequest
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = "Draft";

        // Declared required here. The CASE below always matches a branch, so
        // this is never actually null - but it has no ELSE, which leaves the
        // column itself nullable. That gap between what the model promises and
        // what the schema permits is what SetHistoryColumn exploits.
        public int StatusRank { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;
    }

    public class MyDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TravelRequest> TravelRequests { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model)
        {
            var entity = model.Entity<TravelRequest>();
            entity.ToTable("TravelRequests", _ => _.IsTemporal());
            entity.Property(_ => _.StatusRank)
                .HasComputedColumnSql(
                    """
                    CASE WHEN [Status] = 'Approved' THEN 1
                         WHEN [Status] <> 'Approved' THEN 0 END
                    """,
                    stored: true);
        }
    }

    #endregion

    static SqlInstance<MyDbContext> instance = new(builder => new(builder.Options));

    [OneTimeTearDown]
    public void Cleanup()
    {
        instance.Cleanup();
        instance.Dispose();
    }

    [Test]
    public async Task SetCurrentPeriodStartUsage()
    {
        #region SetCurrentPeriodStartUsage

        await using var database = await instance.Build();

        var request = new TravelRequest { Id = Guid.NewGuid(), Status = "Draft" };
        database.Context.Add(request);
        await database.Context.SaveChangesAsync();

        // Anchor close to "now" so any related entities still exist at that
        // temporal point. Each step must be strictly greater than the previous.
        var anchor = DateTime.UtcNow.AddSeconds(-10);
        await database.SetCurrentPeriodStart(request, anchor);

        request.Status = "ChiefOfStaffReview";
        await database.Context.SaveChangesAsync();
        await database.SetCurrentPeriodStart(request, anchor.AddMilliseconds(100));

        request.Status = "Approved";
        await database.Context.SaveChangesAsync();
        await database.SetCurrentPeriodStart(request, anchor.AddMilliseconds(200));

        // Subsequent TemporalAsOf queries can now resolve each transition by its
        // distinct, deterministic PeriodStart instead of relying on Task.Delay.

        #endregion
    }

    [Test]
    public async Task SetHistoryColumnUsage()
    {
        #region SetHistoryColumnUsage

        await using var database = await instance.Build();

        var request = new TravelRequest { Id = Guid.NewGuid(), Status = "Draft" };
        database.Context.Add(request);
        await database.Context.SaveChangesAsync();

        request.Status = "Approved";
        await database.Context.SaveChangesAsync();

        // Blank the column on the history rows only. A column dropped and
        // re-added on a temporal pair leaves exactly this: the current row is
        // repopulated, the rows already in history are not, and SQL Server
        // never backfills them.
        await database.SetHistoryColumn<TravelRequest>(
            request.Id,
            nameof(TravelRequest.StatusRank),
            null);

        // Materialising that history row now fails on a SqlNullValueException,
        // because the model reads StatusRank into a non-nullable int. That is
        // the production failure, reproduced in a test.
        var exception = CatchAsync(
            () => database.Context.Set<TravelRequest>()
                .TemporalAll()
                .Where(_ => _.Id == request.Id)
                .ToListAsync());
        NotNull(exception);

        #endregion

        // The current row is untouched, so ordinary queries keep working.
        var current = await database.Context.Set<TravelRequest>()
            .Where(_ => _.Id == request.Id)
            .Select(_ => _.StatusRank)
            .SingleAsync();
        AreEqual(1, current);
    }
}