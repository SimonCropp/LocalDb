// Child-process driver for the multi-process race tests.
// Mode "wrapper-start": <wrapper-start> <instanceName> <directory> <signalFile>
//   Reproduces the symmetric race — runs Wrapper.Start once and reports the outcome.
// Mode "killer": <killer> <instanceName> <signalFile> <durationMs>
//   Calls LocalDbApi.StopAndDelete(name) in a tight loop for the given duration.
// Mode "victim": <victim> <instanceName> <signalFile> <durationMs>
//   Opens a SqlConnection to (LocalDb)\name in a tight loop, captures the first
//   exception whose Win32 native error code is 0x89C50107 (LOCALDB_ERROR_INSTANCE_DOES_NOT_EXIST)
//   and exits 0 to signal "race observed". Any other failure exits 1. If no error fires
//   within the duration, exits 2 ("race not observed in window").

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: <mode> <args...>  (mode is wrapper-start | killer | victim)");
    return 64;
}

var mode = args[0];
return mode switch
{
    "wrapper-start" => await RunWrapperStartAsync(args.AsSpan()[1..].ToArray()),
    "killer"        => await RunKillerAsync(args.AsSpan()[1..].ToArray()),
    "victim"        => await RunVictimAsync(args.AsSpan()[1..].ToArray()),
    _               => Fail($"Unknown mode: {mode}")
};

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 64;
}

static async Task<int> RunWrapperStartAsync(string[] args)
{
    if (args.Length < 3)
    {
        return Fail("wrapper-start usage: <instanceName> <directory> <signalFile>");
    }
    var instanceName = args[0];
    var directory = args[1];
    var signalFile = args[2];

    await WaitForSignalAsync(signalFile);

    try
    {
        using var wrapper = new Wrapper(instanceName, directory);
        wrapper.Start(new(2000, 1, 1), _ => Task.CompletedTask);
        await wrapper.AwaitStart();
        Console.Out.WriteLine($"pid {Environment.ProcessId}: success");
        return 0;
    }
    catch (Exception exception)
    {
        ReportException(exception);
        return 1;
    }
}

static async Task<int> RunKillerAsync(string[] args)
{
    if (args.Length < 3)
    {
        return Fail("killer usage: <instanceName> <signalFile> <durationMs>");
    }
    var instanceName = args[0];
    var signalFile = args[1];
    var durationMs = int.Parse(args[2]);

    await WaitForSignalAsync(signalFile);

    var deadline = Environment.TickCount64 + durationMs;
    var killCount = 0;
    while (Environment.TickCount64 < deadline)
    {
        try
        {
            LocalDbApi.StopAndDelete(instanceName);
            killCount++;
        }
        catch
        {
            // Expected — the instance may already be gone, or a victim is using it. Keep hammering.
        }
    }
    Console.Out.WriteLine($"pid {Environment.ProcessId} killer: {killCount} StopAndDelete cycles");
    return 0;
}

static async Task<int> RunVictimAsync(string[] args)
{
    if (args.Length < 3)
    {
        return Fail("victim usage: <instanceName> <signalFile> <durationMs>");
    }
    var instanceName = args[0];
    var signalFile = args[1];
    var durationMs = int.Parse(args[2]);

    await WaitForSignalAsync(signalFile);

    var connectionString = $@"Data Source=(LocalDb)\{instanceName};Initial Catalog=master;Pooling=False;Connect Timeout=2";
    var deadline = Environment.TickCount64 + durationMs;
    var attempts = 0;
    Exception? otherError = null;

    while (Environment.TickCount64 < deadline)
    {
        attempts++;
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
        }
        catch (SqlException sql)
        {
            if (HasNativeCode(sql, unchecked((int)0x89C50107)))
            {
                Console.Out.WriteLine(
                    $"pid {Environment.ProcessId} victim: observed LOCALDB_ERROR_INSTANCE_DOES_NOT_EXIST (0x89C50107) on attempt {attempts}: {FirstLine(sql.Message)}");
                return 0;
            }
            otherError = sql;
        }
        catch (Exception other)
        {
            otherError = other;
        }
    }

    if (otherError == null)
    {
        Console.Error.WriteLine($"pid {Environment.ProcessId} victim: no errors after {attempts} attempts in {durationMs}ms");
        return 2;
    }

    Console.Error.WriteLine($"pid {Environment.ProcessId} victim: {attempts} attempts, no 0x89C50107; last other error: {otherError.GetType().Name}: {FirstLine(otherError.Message)}");
    var inner = otherError.InnerException;
    while (inner != null)
    {
        Console.Error.WriteLine($"  inner: {inner.GetType().Name}: {FirstLine(inner.Message)}");
        inner = inner.InnerException;
    }
    return 1;
}

static bool HasNativeCode(Exception exception, int code)
{
    var current = exception;
    while (current != null)
    {
        if (current is Win32Exception win32 && win32.NativeErrorCode == code)
        {
            return true;
        }
        current = current.InnerException;
    }
    return false;
}

static async Task WaitForSignalAsync(string signalFile)
{
    while (!File.Exists(signalFile))
    {
        await Task.Delay(20);
    }
}

static void ReportException(Exception exception)
{
    Console.Error.WriteLine($"pid {Environment.ProcessId}: {exception.GetType().Name}: {FirstLine(exception.Message)}");
    var inner = exception.InnerException;
    while (inner != null)
    {
        Console.Error.WriteLine($"  inner: {inner.GetType().Name}: {FirstLine(inner.Message)}");
        if (inner is Win32Exception win32)
        {
            Console.Error.WriteLine($"    NativeErrorCode: 0x{win32.NativeErrorCode:X8}");
        }
        inner = inner.InnerException;
    }
}

static string FirstLine(string message) => message.Replace("\r", "").Split('\n')[0];
