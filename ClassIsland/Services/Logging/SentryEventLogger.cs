using System;
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
        var message = formatter(state, exception);
        // switch (logLevel)
        // {
        //     case LogLevel.Trace:
        //         SentrySdk.Logger.LogTrace(message);
        //         break;
        //     case LogLevel.Debug:
        //         SentrySdk.Logger.LogDebug(message);
        //         break;
        //     case LogLevel.Information:
        //         SentrySdk.Logger.LogInfo(message);
        //         break;
        //     case LogLevel.Warning:
        //         SentrySdk.Logger.LogWarning(message);
        //         break;
        //     case LogLevel.Error:
        //         SentrySdk.Logger.LogError(message);
        //         break;
        //     case LogLevel.Critical:
        //         SentrySdk.Logger.LogFatal(message);
        //         break;
        //     case LogLevel.None:
        //         break;
        //     default:
        //         throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        // }
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