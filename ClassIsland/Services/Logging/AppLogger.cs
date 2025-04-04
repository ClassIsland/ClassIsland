using System;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.Logging;
using Microsoft.Extensions.Logging;
using Pastel;

namespace ClassIsland.Services.Logging;

public class AppLogger(AppLogService appLogService, string categoryName) : ILogger
{
    private AppLogService AppLogService { get; } = appLogService;

    private string CategoryName { get; } = categoryName;
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception) + (exception != null ? "\n" + exception : "");
        AppLogService.AddLog(new LogEntry()
        {
            LogLevel = logLevel,
            Message = LogMaskingHelper.MaskLog(message),
            CategoryName = CategoryName,
            Exception = exception
        });
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