// ReSharper disable PartialTypeWithSinglePart
#if EF
using EfLocalDb;
#else
using LocalDb;
#endif

static partial class LocalDbApi
{
    static IntPtr api;

    static LocalDbApi()
    {
        var (path, version) = LocalDbRegistryReader.GetInfo();
        ApiVersion = version;
        uint loadLibrarySearchDefaultDirs = 0x00001000;
        api = LoadLibraryEx(path, IntPtr.Zero, loadLibrarySearchDefaultDirs);
        if (api == IntPtr.Zero)
        {
            throw new Win32Exception();
        }

        createInstance = GetFunction<LocalDBCreateInstance>();
        getInstanceInfo = GetFunction<LocalDBGetInstanceInfo>();
        getInstances = GetFunction<LocalDBGetInstances>();
        deleteInstance = GetFunction<LocalDBDeleteInstance>();
        startInstance = GetFunction<LocalDBStartInstance>();
        stopInstance = GetFunction<LocalDBStopInstance>();
    }

    static string ApiVersion;
    public const int MaxPath = 260;
    public const int MaxName = 129;
    public const int MaxSid = 187;

    static LocalDBCreateInstance createInstance;
    static LocalDBGetInstanceInfo getInstanceInfo;
    static LocalDBGetInstances getInstances;
    static LocalDBDeleteInstance deleteInstance;
    static LocalDBStartInstance startInstance;
    static LocalDBStopInstance stopInstance;

    static T GetFunction<T>()
        where T : class
    {
        var name = typeof(T).Name;
        var ptr = GetProcAddress(api, name);
        if (ptr == IntPtr.Zero)
        {
            throw new EntryPointNotFoundException(name);
        }

        object function = Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
        return (T) function;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int LocalDBDeleteInstance(
        [MarshalAs(UnmanagedType.LPWStr)] string pInstanceName,
        int dwFlags);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int LocalDBCreateInstance(
        [MarshalAs(UnmanagedType.LPWStr)] string wszVersion,
        [MarshalAs(UnmanagedType.LPWStr)] string pInstanceName,
        int dwFlags);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int LocalDBGetInstanceInfo(
        [MarshalAs(UnmanagedType.LPWStr)] string wszInstanceName,
        ref LocalDbInstanceInfo pInstanceInfo,
        int dwInstanceInfoSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int LocalDBGetInstances(
        IntPtr pInstanceNames,
        ref int lpdwNumberOfInstances);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int LocalDBStartInstance(
        [MarshalAs(UnmanagedType.LPWStr)] string pInstanceName,
        int dwFlags,
        [MarshalAs(UnmanagedType.LPWStr)] [Out]
        StringBuilder wszSqlConnection,
        ref int lpcchSqlConnection);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate int LocalDBStopInstance(
        [MarshalAs(UnmanagedType.LPWStr)] string pInstanceName,
        int dwFlags,
        int ulTimeout);

#if NET7_0_OR_GREATER
    [LibraryImport(
        "kernel32",
        SetLastError = true,
        StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [LibraryImport(
        "kernel32",
        SetLastError = true,
        EntryPoint = "LoadLibraryExA",
        StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, uint dwFlags);
#else
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    [DllImport(
        "kernel32",
        BestFitMapping = false,
        SetLastError = false,
        ThrowOnUnmappableChar = true,
        CharSet = CharSet.Ansi)]
    static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    [DllImport(
        "kernel32",
        BestFitMapping = false,
        SetLastError = false,
        ThrowOnUnmappableChar = true,
        CharSet = CharSet.Ansi)]
    static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, uint dwFlags);
#endif

    public static List<string> GetInstanceNames()
    {
        var count = 0;
        getInstances(IntPtr.Zero, ref count);
        var length = MaxName * sizeof(char);
        var pointer = Marshal.AllocHGlobal(count * length);
        try
        {
            getInstances(pointer, ref count);
            var names = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                var idx = IntPtr.Add(pointer, length * i);
                names.Add(Marshal.PtrToStringAuto(idx)!);
            }

            return names;
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    static int localDbInstanceInfoSize = Marshal.SizeOf(typeof(LocalDbInstanceInfo));

    public static LocalDbInstanceInfo GetInstance(string instanceName)
    {
        var info = new LocalDbInstanceInfo();
        getInstanceInfo(instanceName, ref info, localDbInstanceInfoSize);
        return info;
    }

    public static void DeleteInstance(string instanceName) => deleteInstance(instanceName, 0);

    static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(10);

    public static void StopAndDelete(string instanceName, ShutdownMode mode = ShutdownMode.KillProcess) =>
        StopAndDelete(instanceName, mode, DefaultShutdownTimeout);

    public static void StopAndDelete(string instanceName, ShutdownMode mode, TimeSpan timeout)
    {
        StopInstance(instanceName, mode, timeout);
        DeleteInstance(instanceName);
    }

    public static void CreateInstance(string instanceName) =>
        createInstance(ApiVersion, instanceName, 0);

    public static State CreateAndStart(string instance)
    {
        var localDbInstanceInfo = GetInstance(instance);

        if (!localDbInstanceInfo.Exists)
        {
            CreateInstance(instance);
            StartInstance(instance);
            return State.NotExists;
        }

        if (!localDbInstanceInfo.IsRunning)
        {
            StartInstance(instance);
        }

        return State.Running;
    }

    public static void StartInstance(string instanceName)
    {
        var connection = new StringBuilder(MaxPath);
        var size = connection.Capacity;

        startInstance(instanceName, 0, connection, ref size);
    }

    public static void StopInstance(string instanceName, ShutdownMode mode = ShutdownMode.UseSqlShutdown) =>
        StopInstance(instanceName, mode, DefaultShutdownTimeout);

    public static void StopInstance(string instanceName, ShutdownMode mode, TimeSpan timeout) =>
        stopInstance(instanceName, (int)mode, (int)timeout.TotalMilliseconds);
}