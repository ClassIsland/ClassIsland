using System;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace ClassIsland.Services.Logging;

public class FileLogger(FileLoggerProvider provider, string categoryName) : ILogger
{
    private FileLoggerProvider Provider { get; } = provider;
    private string CategoryName { get; } = categoryName;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception) + (exception != null ? "\n" + exception : "");
        Provider.WriteLog($"{DateTime.Now}|{logLevel}|{CategoryName}|{message}");
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}