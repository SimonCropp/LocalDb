[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class LocalDbTestBase<T>
    where T : DbContext
{
    Phase phase = Phase.Arrange;
    static SqlInstance<T> sqlInstance = null!;
    T actData = null!;

    public static void Initialize(
        ConstructInstance<T>? constructInstance = null,
        TemplateFromContext<T>? buildTemplate = null,
        ushort templateSize = 10,
        Callback<T>? callback = null) =>
        sqlInstance = new(
            buildTemplate: buildTemplate,
            constructInstance: builder =>
            {
                builder.EnableRecording();
                if (constructInstance != null)
                {
                    return constructInstance(builder);
                }

                var type = typeof(T);
                try
                {
                    return (T)Activator.CreateInstance(type, builder.Options)!;
                }
                catch (Exception exception)
                {
                    throw new($"Could not construct instance of T ({type.Name}). Either provide a constructInstance delegate or ensure T has a constructor that accepts DbContextOptions.", exception);
                }
            },
            storage: Storage.FromSuffix<T>($"{AttributeReader.GetSolutionName()}_{AttributeReader.GetProjectName()}"),
            templateSize: templateSize,
            callback: callback);

    public SqlDatabase<T> Database { get; private set; } = null!;

    public T ArrangeData
    {
        get
        {
            if (phase != Phase.Arrange)
            {
                throw new($"Phase has already moved to {phase}");
            }

            return Database.Context;
        }
    }

    public T ActData
    {
        get
        {
            if (phase == Phase.Act)
            {
                return actData;
            }

            if (phase == Phase.Assert)
            {
                throw new("Phase has already moved to Assert");
            }

            Recording.Start();
            phase = Phase.Act;
            QueryFilter.Enable();
            return actData;
        }
    }

    public T AssertData
    {
        get
        {
            if (phase == Phase.Assert)
            {
                return Database.NoTrackingContext;
            }

            phase = Phase.Assert;

            QueryFilter.Disable();
            if (Recording.IsRecording())
            {
                Recording.Pause();
            }

            return Database.NoTrackingContext;
        }
    }

    protected LocalDbTestBase() =>
        // Disable needs to be at the top of the AsyncLocal stack
        QueryFilter.Disable();

    [SetUp]
    public async Task SetUp()
    {
        if (sqlInstance == null)
        {
            throw new("Call LocalDbTestBase<T>.Initialize in a ModuleInitializer.");
        }

        QueryFilter.Disable();
        var test = TestContext.CurrentContext.Test;
        var arguments = string.Join(
            ' ',
            test.Arguments.Select(VerifierSettings.GetNameForParameter));
        var type = test.ClassName!;
        var member = $"{test.MethodName}_{arguments}";
        Database = await sqlInstance.Build(type, null, member);
        Database.NoTrackingContext.DisableRecording();
        Database.Context.DisableRecording();
        actData = Database.NewDbContext();
    }

    [TearDown]
    public async ValueTask TearDown()
    {
        await Database.DisposeAsync();
        await actData.DisposeAsync();
    }

    [Pure]
    public SettingsTask VerifyEntity<TEntity>(Guid id, [CallerFilePath] string sourceFile = "")
        where TEntity : class =>
        InnerVerifyEntity<TEntity>(id, sourceFile);

    [Pure]
    public SettingsTask VerifyEntities<TEntity>(IQueryable<TEntity> entities, [CallerFilePath] string sourceFile = "")
        where TEntity : class =>
        Verify(entities.ToList(), sourceFile: sourceFile);

    [Pure]
    public SettingsTask VerifyEntity<TEntity>(long id, [CallerFilePath] string sourceFile = "")
        where TEntity : class =>
        InnerVerifyEntity<TEntity>(id, sourceFile);

    [Pure]
    public SettingsTask VerifyEntity<TEntity>(int id, [CallerFilePath] string sourceFile = "")
        where TEntity : class =>
        InnerVerifyEntity<TEntity>(id, sourceFile);

    SettingsTask InnerVerifyEntity<TEntity>(object id, string sourceFile)
        where TEntity : class
    {
        var set = AssertData.Set<TEntity>();
        var entity = set.FindAsync(id);
        return Verify(entity, sourceFile: sourceFile);
    }
}