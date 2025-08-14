namespace EfLocalDbNunit;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract partial class LocalDbTestBase<T> :
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
        instance.Value = this;
    }

    static AsyncLocal<LocalDbTestBase<T>?> instance = new();

    public static LocalDbTestBase<T> Instance
    {
        get
        {
            var value = instance.Value;
            if (value == null)
            {
                throw new("No current value");
            }

            return value;
        }
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

        if (actData != null)
        {
            await actData.DisposeAsync();
        }

        if (Database != null)
        {
            if (BuildServerDetector.Detected)
            {
                LocalDbLogging.Log($"Purging {Database.Name}");
                await Database.Delete();
            }
            else
            {
                await Database.DisposeAsync();
            }
        }

        // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        instance.Value = null;
    }
}