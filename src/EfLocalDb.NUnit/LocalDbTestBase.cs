namespace EfLocalDbNunit;

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
        Callback<T>? callback = null)
    {
        ThrowIfInitialized();
        sqlInstance = new(
            buildTemplate: buildTemplate,
            constructInstance: builder =>
            {
                builder.EnableRecording();
                return constructInstance == null ? BuildDbContext(builder) : constructInstance(builder);
            },
            storage: GetStorage(),
            templateSize: templateSize,
            callback: callback);
    }

    static void ThrowIfInitialized()
    {
        if (sqlInstance != null)
        {
            throw new("Already initialized.");
        }
    }

    static T BuildDbContext(DbContextOptionsBuilder<T> builder)
    {
        var type = typeof(T);
        try
        {
            return (T)Activator.CreateInstance(type, builder.Options)!;
        }
        catch (Exception exception)
        {
            throw new($"Could not construct instance of T ({type.Name}). Either provide a constructInstance delegate or ensure T has a constructor that accepts DbContextOptions.", exception);
        }
    }

    static Storage GetStorage() =>
        Storage.FromSuffix<T>($"{AttributeReader.GetSolutionName()}_{AttributeReader.GetProjectName()}");

    public SqlDatabase<T> Database { get; private set; } = null!;

    public virtual T ArrangeData
    {
        get
        {
            if (phase == Phase.Act)
            {
                throw new("Phase has already moved to Act. Check for a ActData usage in the preceding code.");
            }

            if (phase == Phase.Assert)
            {
                throw new("Phase has already moved to Assert. Check for a AssertData usage in the preceding code.");
            }

            return Database.Context;
        }
    }

    public virtual T ActData
    {
        get
        {
            if (phase == Phase.Act)
            {
                return actData;
            }

            if (phase == Phase.Assert)
            {
                throw new("Phase has already moved to Assert. Check for a AssertData usage in the preceding code.");
            }

            Recording.Resume();
            phase = Phase.Act;
            QueryFilter.Enable();
            return actData;
        }
    }

    public virtual T AssertData
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

    protected LocalDbTestBase()
    {
        // Disable and Recording needs to be at the top of the AsyncLocal stack
        QueryFilter.Disable();
        Recording.Start();
        Recording.Pause();
    }

    [SetUp]
    public virtual async Task SetUp()
    {
        if (sqlInstance == null)
        {
            throw new("Call LocalDbTestBase<T>.Initialize in a [ModuleInitializer] or in a static constructor.");
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
    public virtual async ValueTask TearDown()
    {
        await Database.DisposeAsync();
        await actData.DisposeAsync();
    }

    [Pure]
    public virtual SettingsTask VerifyEntity<TEntity>(Guid id, [CallerFilePath] string sourceFile = "")
        where TEntity : class =>
        InnerVerifyEntity<TEntity>(id, sourceFile);

    [Pure]
    public virtual SettingsTask VerifyEntity<TEntity>(IQueryable<TEntity> entities, [CallerFilePath] string sourceFile = "")
        where TEntity : class =>
        Verify(entities.SingleAsync(), sourceFile: sourceFile);

    [Pure]
    public virtual SettingsTask VerifyEntities<TEntity>(IQueryable<TEntity> entities, [CallerFilePath] string sourceFile = "")
        where TEntity : class =>
        Verify(entities.ToListAsync(), sourceFile: sourceFile);

    [Pure]
    public virtual SettingsTask VerifyEntity<TEntity>(long id, [CallerFilePath] string sourceFile = "")
        where TEntity : class =>
        InnerVerifyEntity<TEntity>(id, sourceFile);

    [Pure]
    public virtual SettingsTask VerifyEntity<TEntity>(int id, [CallerFilePath] string sourceFile = "")
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