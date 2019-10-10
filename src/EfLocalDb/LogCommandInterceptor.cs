using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

class LogCommandInterceptor :
    DbCommandInterceptor
{
    static void WriteLine(DbCommand command, [CallerMemberName] string member = "")
    {
        Trace.WriteLine($@"EF {member}:
{command.CommandText}", "LocalDB");
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData data, InterceptionResult<DbDataReader> result)
    {
        WriteLine(command);
        return result;
    }

    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData data, InterceptionResult<object> result)
    {
        WriteLine(command);
        return result;
    }

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData data, InterceptionResult<int> result)
    {
        WriteLine(command);
        return result;
    }

    public override Task<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData data, InterceptionResult<DbDataReader> result, CancellationToken cancellation)
    {
        WriteLine(command);
        return Task.FromResult(result);
    }

    public override Task<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData data, InterceptionResult<object> result, CancellationToken cancellation)
    {
        WriteLine(command);
        return Task.FromResult(result);
    }

    public override Task<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData data, InterceptionResult<int> result, CancellationToken cancellation)
    {
        WriteLine(command);
        return Task.FromResult(result);
    }
}