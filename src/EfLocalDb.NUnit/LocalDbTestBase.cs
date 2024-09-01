[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class LocalDbTestBase<T>
    where T : DbContext
{
    static SqlInstance<T> sqlInstance = null!;
    T actData = null!;

    public static void Initialize(
        ConstructInstance<T> constructInstance,
        TemplateFromContext<T>? buildTemplate = null,
        ushort templateSize = 3,
        Callback<T>? callback = null) =>
        sqlInstance = new(
            buildTemplate: buildTemplate,
            constructInstance: builder =>
            {
                builder.EnableRecording();
                return constructInstance(builder);
            },
            storage: Storage.FromSuffix<T>($"{AttributeReader.GetSolutionName()}_{AttributeReader.GetProjectName()}"),
            templateSize: templateSize,
            callback: callback);

    public SqlDatabase<T> Database { get; private set; } = null!;
    public T ArrangeData => Database.Context;

    public T ActData
    {
        get
        {
            QueryFilter.Enable();
            return actData;
        }
        private set => actData = value;
    }

    public T AssertData
    {
        get
        {
            QueryFilter.Disable();
            if (Recording.IsRecording())
            {
                Recording.Pause();
            }

            return Database.NoTrackingContext;
        }
    }

    public LocalDbTestBase() =>
        QueryFilter.Disable();

    [SetUp]
    public async Task SetUp()
    {
        var test = TestContext.CurrentContext.Test;
        var arguments = string.Join(
            ' ',
            test.Arguments.Select(VerifierSettings.GetNameForParameter));
        var type = test.ClassName!;
        var member = $"{test.MethodName}_{arguments}";
        Database = await sqlInstance.Build(type, null, member);
        Database.NoTrackingContext.DisableRecording();
        Database.Context.DisableRecording();
        ActData = Database.NewDbContext();
    }

    [TearDown]
    public async ValueTask TearDown()
    {
        await Database.DisposeAsync();
        await ActData.DisposeAsync();
    }

    [Pure]
    public SettingsTask VerifyEntity<TEntity>(Guid id, [CallerFilePath] string sourceFile = "")
        where TEntity : class
    {
        var set = AssertData.Set<TEntity>();
        var entity = set.FindAsync(id);
        return Verify(entity, sourceFile: sourceFile);
    }
}