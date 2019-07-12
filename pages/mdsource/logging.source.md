# Logging

By default some information is written to [Trace.WriteLine](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.trace.writeline#System_Diagnostics_Trace_WriteLine_System_String_System_String_)

 * The SqlLocalDb instance name when `SqlInstance` is instantiated.
 * The database name when a `SqlDatabase` is built.

To enable verbose logging use `LocalDbLogging`:

snippet: LocalDbLoggingUsage

The full implementation is:

snippet: LocalDbLogging

Which is then combined with [Fody MethodTimer](https://github.com/Fody/MethodTimer):

snippet: MethodTimeLogger


## Logging in xUnit

xUnit does not route `Trace.WriteLine` to [ITestOutputHelper](https://xunit.net/docs/capturing-output). Tracking issue: [3.0: Console / Trace / Debugger capture support ](https://github.com/xunit/xunit/issues/1730).

This can be worked around by [adding a Trace Listener(https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.trace.listeners) that writes to `ITestOutputHelper`. Or alternatively use [XunitLogger](https://github.com/SimonCropp/XunitLogger).