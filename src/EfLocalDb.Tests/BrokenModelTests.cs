// The EF model build runs concurrently with the LocalDB instance start (see the SqlInstance
// constructor), so a broken model surfaces only after Wrapper.Start has run. Pins that
// failure mode: the original exception escapes the constructor (not AggregateException),
// and the already-started instance is left in a cleanable state.
[TestFixture]
public class BrokenModelTests
{
    class BrokenModelDbContext(DbContextOptions options) :
        DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder model) =>
            throw new InvalidOperationException("Deliberate model failure");
    }

    [Test]
    public void ThrowsOriginalExceptionFromConstructor()
    {
        var storage = Storage.FromSuffix<BrokenModelDbContext>("BrokenModel");
        try
        {
            var exception = Throws<InvalidOperationException>(
                () => new SqlInstance<BrokenModelDbContext>(
                    builder => new(builder.Options),
                    storage: storage));
            That(exception!.Message, Does.Contain("Deliberate model failure"));

            // the instance started before the model build was joined
            That(LocalDbApi.GetInstance(storage.Name).Exists, Is.True);
        }
        finally
        {
            LocalDbApi.StopAndDelete(storage.Name);
            if (Directory.Exists(storage.Directory))
            {
                Directory.Delete(storage.Directory, true);
            }
        }
    }
}
