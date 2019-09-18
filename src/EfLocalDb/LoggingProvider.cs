using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

class LoggingProvider :
    ILoggerProvider,
    ILogger
{
    public static readonly LoggerFactory LoggerFactory
        = new LoggerFactory(new[] {new LoggingProvider()});

    public ILogger CreateLogger(string categoryName)
    {
        if (DbLoggerCategory.Database.Command.Name == categoryName)
        {
            return this;
        }

        return NullLogger.Instance;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        //if (eventId.Id != RelationalStrings.LogRelationalLoggerExecutedCommand.EventId.Id)
        //{
        //    return;
        //}

        Trace.WriteLine($@"Executed EF SQL command:
{state.ToString().IndentLines()}", "LocalDB");
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable BeginScope<TState>(TState state) => null;

    public void Dispose()
    {
    }
}