using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.Logging;
using ClassIsland.Models.Logging;
using Microsoft.Extensions.Logging;
using Pastel;

namespace ClassIsland.Services.Logging;

public class AppLogger(AppLogService appLogService, string categoryName) : ILogger
{
    private AppLogService AppLogService { get; } = appLogService;

    private string CategoryName { get; } = categoryName;

    private static readonly AsyncLocal<Stack<object>> ScopeStack = new AsyncLocal<Stack<object>>();

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var snapshot = ScopeStack.Value?.ToArray() ?? [];
        var scopes = snapshot.Select(scope => (scope?.ToString() ?? "") + " => ").ToList();
    
        var message = string.Join("", scopes) + formatter(state, exception)
                                              + (exception != null ? "\n" + exception : "");
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
        ScopeStack.Value ??= new Stack<object>();
        ScopeStack.Value.Push(state);

        return new LoggingScope(() => ScopeStack.Value.Pop());
    }
}