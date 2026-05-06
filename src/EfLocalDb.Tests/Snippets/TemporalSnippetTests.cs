using System.ComponentModel.DataAnnotations;

public class TemporalSnippetTests
{
    #region TemporalEntityConfig

    public class TravelRequest
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = "Draft";

        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;
    }

    public class MyDbContext(DbContextOptions options) :
        DbContext(options)
    {
        public DbSet<TravelRequest> TravelRequests { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder model) =>
            model.Entity<TravelRequest>()
                .ToTable("TravelRequests", _ => _.IsTemporal());
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
        await database.Context.SetCurrentPeriodStart(request, anchor);

        request.Status = "ChiefOfStaffReview";
        await database.Context.SaveChangesAsync();
        await database.Context.SetCurrentPeriodStart(request, anchor.AddMilliseconds(100));

        request.Status = "Approved";
        await database.Context.SaveChangesAsync();
        await database.Context.SetCurrentPeriodStart(request, anchor.AddMilliseconds(200));

        // Subsequent TemporalAsOf queries can now resolve each transition by its
        // distinct, deterministic PeriodStart instead of relying on Task.Delay.

        #endregion
    }
}
