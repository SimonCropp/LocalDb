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
        if (eventId.Id != RelationalStrings.LogRelationalLoggerExecutingCommand.EventId.Id)
        {
            return;
        }

        Trace.WriteLine($@"Executing EF SQL command:
{state}", "LocalDB");
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return null;
    }

    public void Dispose()
    {
    }
}