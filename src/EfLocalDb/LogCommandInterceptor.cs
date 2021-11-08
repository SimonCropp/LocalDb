using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

class LogCommandInterceptor :
    DbCommandInterceptor
{
    static void WriteLine(CommandEventData data)
    {
        LocalDbLogging.Log($"EF {data}");
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData data)
    {
        WriteLine(data);
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData data, CancellationToken cancellation = default)
    {
        WriteLine(data);
        return Task.CompletedTask;
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData data, DbDataReader result)
    {
        WriteLine(data);
        return result;
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData data, object? result)
    {
        WriteLine(data);
        return result;
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData data, int result)
    {
        WriteLine(data);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData data, DbDataReader result, CancellationToken cancellation = default)
    {
        WriteLine(data);
        return new(result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData data, object? result, CancellationToken cancellation = default)
    {
        WriteLine(data);
        return new(result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData data, int result, CancellationToken cancellation = default)
    {
        WriteLine(data);
        return new(result);
    }
}