using DiffEngine;

namespace EfLocalDbNunit;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class LocalDbTestBase<T> :
    ILocalDbTestBase
    where T : DbContext
{
    Phase phase = Phase.Arrange;
    static SqlInstance<T> sqlInstance = null!;
    T actData = null!;
    T arrangeData = null!;

    public static void Initialize(
        ConstructInstance<T>? constructInstance = null,
        TemplateFromContext<T>? buildTemplate = null,
        ushort templateSize = 10,
        Callback<T>? callback = null)
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        ThrowIfInitialized();
        sqlInstance = new(
            buildTemplate: buildTemplate,
            constructInstance: builder =>
            {
                builder.EnableRecording();
                return constructInstance == null ? BuildDbContext(builder) : constructInstance(builder);
            },
            storage: GetStorage(callingAssembly),
            templateSize: templateSize,
            callback: callback);
    }

    [SetUp]
    public virtual Task SetUp()
    {
        if (sqlInstance == null)
        {
            throw new("Call LocalDbTestBase<T>.Initialize in a [ModuleInitializer] or in a static constructor.");
        }

        QueryFilter.Enable();
        return Reset();
    }

    public async Task Reset()
    {
        phase = Phase.Arrange;
        var test = TestContext.CurrentContext.Test;
        var type = test.ClassName!;
        var member = GetMemberName(test);
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if(Database != null)
        {
            await Database.Delete();
            await Database.DisposeAsync();
        }
        Database = await sqlInstance.Build(type, null, member);
        Database.NoTrackingContext.DisableRecording();
        arrangeData = Database.Context;
        arrangeData.DisableRecording();
        actData = Database.NewDbContext();
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

    static Storage GetStorage(Assembly callingAssembly) =>
        Storage.FromSuffix<T>($"{AttributeReader.GetSolutionName(callingAssembly)}_{AttributeReader.GetProjectName(callingAssembly)}");

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

            return arrangeData;
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
            arrangeData.Dispose();
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
            arrangeData.Dispose();
            actData.Dispose();

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
        CombinationCallback.SetInstance(this);
        // Enable and Recording needs to be at the top of the AsyncLocal stack
        QueryFilter.Enable();
        Recording.Start();
        Recording.Pause();
    }

    static string GetMemberName(TestContext.TestAdapter test)
    {
        var method = test.MethodName!;
        if (test.Arguments.Length == 0)
        {
            return method;
        }

        var arguments = string.Join(
            ' ',
            test.Arguments.Select(VerifierSettings.GetNameForParameter));
        return $"{method}_{arguments}";
    }

    [TearDown]
    public virtual async ValueTask TearDown()
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Database != null)
        {
            await Database.DisposeAsync();
        }

        if (actData != null)
        {
            await actData.DisposeAsync();
        }
        // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    }

    [OneTimeTearDown]
    public static void OneTimeTearDown()
    {
        if (!BuildServerDetector.Detected)
        {
            return;
        }

        var directory = sqlInstance.StorageDirectory;
        LocalDbLogging.Log($"Purging {directory}");
        sqlInstance.Cleanup(ShutdownMode.UseSqlShutdown);
    }

    [Pure]
    public virtual SettingsTask VerifyEntity<TEntity>(Guid id, [CallerFilePath] string sourceFile = "")
        where TEntity : class =>
        InnerVerifyEntity<TEntity>(id, sourceFile);

    [Pure]
    public virtual SettingsTask VerifyEntity<TEntity>(IQueryable<TEntity> entities, [CallerFilePath] string sourceFile = "") =>
        Verify(ResolveSingle(entities), sourceFile: sourceFile);

    static async Task<TEntity> ResolveSingle<TEntity>(IQueryable<TEntity> entities)
    {
        try
        {
            return await entities.SingleAsync();
        }
        catch (ObjectDisposedException exception)
        {
            throw NewDisposedException(exception);
        }
    }

    [Pure]
    public virtual SettingsTask VerifyEntities<TEntity>(IQueryable<TEntity> entities, [CallerFilePath] string sourceFile = "") =>
        Verify(ResolveList(entities), sourceFile: sourceFile);

    static async Task<List<TEntity>> ResolveList<TEntity>(IQueryable<TEntity> entities)
    {
        try
        {
            return await entities.ToListAsync();
        }
        catch (ObjectDisposedException exception)
        {
            throw NewDisposedException(exception);
        }
    }

    static Exception NewDisposedException(ObjectDisposedException exception) =>
        new("ObjectDisposedException while executing IQueryable. It is possible the IQueryable targets an ActData or ArrangeData that has already been cleaned up", exception);

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