using System;
using ClassIsland.Core.Models.Logging;
using Microsoft.Extensions.Logging;
using Sentry;

namespace ClassIsland.Services.Logging;

public class SentryEventLogger(string categoryName) : ILogger
{
    private string CategoryName { get; } = categoryName;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel >= LogLevel.Error && exception != null)
        {
            SentrySdk.CaptureException(exception, scope =>
            {
                scope.Level = logLevel switch
                {
                    LogLevel.Warning => SentryLevel.Warning,
                    LogLevel.Error => SentryLevel.Error,
                    LogLevel.Critical => SentryLevel.Fatal,
                    _ => SentryLevel.Info
                };
            });
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}