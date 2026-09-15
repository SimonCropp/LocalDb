using System.Reflection.Emit;

[TestFixture]
public class DbAttributeReaderTests
{
    [AttributeUsage(AttributeTargets.All)]
    public sealed class SharedAttribute : Attribute;

    [AttributeUsage(AttributeTargets.All)]
    public sealed class PooledAttribute : Attribute;

    [AttributeUsage(AttributeTargets.All)]
    public sealed class NewAttribute : Attribute;

    public class Plain
    {
        public static void None()
        {
        }

        [Shared]
        public static void Shared()
        {
        }

        [Pooled]
        public static void Pooled()
        {
        }

        [Shared, Pooled]
        public static void SharedAndPooled()
        {
        }

        [Pooled, New]
        public static void PooledAndNew()
        {
        }
    }

    [Shared]
    public class SharedClass
    {
        public static void None()
        {
        }

        [Pooled]
        public static void Pooled()
        {
        }

        [New]
        public static void New()
        {
        }
    }

    [New]
    public class NewClass
    {
        public static void None()
        {
        }

        [Pooled]
        public static void Pooled()
        {
        }
    }

    [Shared, Pooled]
    public class BothClass
    {
        [Pooled]
        public static void Pooled()
        {
        }
    }

    public class DerivedFromSharedClass : SharedClass;

    static Assembly noneAssembly = typeof(DbAttributeReaderTests).Assembly;
    static Assembly pooledAssembly = BuildAssembly(typeof(PooledAttribute));
    static Assembly bothAssembly = BuildAssembly(typeof(SharedAttribute), typeof(PooledAttribute));

    static Assembly BuildAssembly(params Type[] attributes) =>
        AssemblyBuilder.DefineDynamicAssembly(
            new($"DbAttributeReaderTests_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.Run,
            attributes.Select(_ => new CustomAttributeBuilder(_.GetConstructor(Type.EmptyTypes)!, [])));

    static DbMode Read<T>(string method, Assembly assembly) =>
        DbAttributeReader.Read<SharedAttribute, PooledAttribute, NewAttribute>(
            typeof(T).GetMethod(method, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!,
            typeof(T),
            assembly);

    [Test]
    public void NoAttributes() =>
        AreEqual(DbMode.New, Read<Plain>("None", noneAssembly));

    [Test]
    public void MethodLevel()
    {
        AreEqual(DbMode.Shared, Read<Plain>("Shared", noneAssembly));
        AreEqual(DbMode.Pooled, Read<Plain>("Pooled", noneAssembly));
    }

    [Test]
    public void ClassLevel() =>
        AreEqual(DbMode.Shared, Read<SharedClass>("None", noneAssembly));

    [Test]
    public void ClassLevelIsInherited() =>
        AreEqual(DbMode.Shared, Read<DerivedFromSharedClass>("None", noneAssembly));

    [Test]
    public void AssemblyLevel() =>
        AreEqual(DbMode.Pooled, Read<Plain>("None", pooledAssembly));

    [Test]
    public void MethodOverridesClass() =>
        AreEqual(DbMode.Pooled, Read<SharedClass>("Pooled", noneAssembly));

    [Test]
    public void MethodOverridesAssembly() =>
        AreEqual(DbMode.Shared, Read<Plain>("Shared", pooledAssembly));

    [Test]
    public void ClassOverridesAssembly() =>
        AreEqual(DbMode.Shared, Read<SharedClass>("None", pooledAssembly));

    [Test]
    public void NewMethodOptsOutOfClass() =>
        AreEqual(DbMode.New, Read<SharedClass>("New", noneAssembly));

    [Test]
    public void NewClassOptsOutOfAssembly() =>
        AreEqual(DbMode.New, Read<NewClass>("None", pooledAssembly));

    [Test]
    public void MethodOverridesNewClass() =>
        AreEqual(DbMode.Pooled, Read<NewClass>("Pooled", noneAssembly));

    [Test]
    public void SharedAndPooledOnMethodThrows()
    {
        var exception = Throws<Exception>(() => Read<Plain>("SharedAndPooled", noneAssembly))!;
        AreEqual("[PooledDb], [SharedDb] and [NewDb] are mutually exclusive. Use only one on a test method.", exception.Message);
    }

    [Test]
    public void PooledAndNewOnMethodThrows()
    {
        var exception = Throws<Exception>(() => Read<Plain>("PooledAndNew", noneAssembly))!;
        AreEqual("[PooledDb], [SharedDb] and [NewDb] are mutually exclusive. Use only one on a test method.", exception.Message);
    }

    [Test]
    public void BothOnClassThrows()
    {
        var exception = Throws<Exception>(() => Read<BothClass>("Pooled", noneAssembly))!;
        AreEqual("[PooledDb], [SharedDb] and [NewDb] are mutually exclusive. Use only one on a test class.", exception.Message);
    }

    [Test]
    public void BothOnAssemblyThrows()
    {
        var exception = Throws<Exception>(() => Read<Plain>("Pooled", bothAssembly))!;
        AreEqual("[PooledDb], [SharedDb] and [NewDb] are mutually exclusive. Use only one on an assembly.", exception.Message);
    }
}
