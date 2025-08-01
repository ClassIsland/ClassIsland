using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ClassIsland.Core.Helpers;
using ClassIsland.Models.Logging;
using Microsoft.Extensions.Logging;
using Pastel;

namespace ClassIsland.Services.Logging;

public class FileLogger(FileLoggerProvider provider, string categoryName) : ILogger
{
    private static readonly AsyncLocal<Stack<object>> ScopeStack = new AsyncLocal<Stack<object>>();
    private FileLoggerProvider Provider { get; } = provider;
    private string CategoryName { get; } = categoryName;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var snapshot = ScopeStack.Value?.ToArray() ?? [];
        var scopes = snapshot.Select(scope => (scope?.ToString() ?? "") + " => ").ToList();
    
        var message = string.Join("", scopes) + formatter(state, exception)
                                              + (exception != null ? "\n" + exception : "");
        message = LogMaskingHelper.MaskLog(message);
        Provider.WriteLog($"{DateTime.Now}|{logLevel}|{CategoryName}|{message}");
    }


    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        ScopeStack.Value ??= new Stack<object>();
        ScopeStack.Value.Push(state);

        return new LoggingScope(() => ScopeStack.Value.Pop());
    }
}