﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using ClassIsland.Core.Helpers;
using ClassIsland.Models.Logging;
using Microsoft.Extensions.Logging;
using Pastel;

namespace ClassIsland.Services.Logging;

public class FileLogger(FileLoggerProvider provider, string categoryName) : ILogger
{
    private static readonly AsyncLocal<ImmutableStack<object>> ScopeStack = new();
    private FileLoggerProvider Provider { get; } = provider;
    private string CategoryName { get; } = categoryName;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var scopes = new List<string>();
        if (ScopeStack.Value != null)
        {
            scopes.AddRange(ScopeStack.Value.Select(scope => (scope.ToString() ?? "") + "=>"));
        }
        var message = string.Join("", scopes) + formatter(state, exception) + (exception != null ? Environment.NewLine + exception : "");
        message = LogMaskingHelper.MaskLog(message);
        Provider.WriteLog($"{DateTime.Now}|{logLevel}|{CategoryName}|{message}");
    }


    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        var previous = ScopeStack.Value;
        var newStack = (previous ?? ImmutableStack<object>.Empty).Push(state);
        ScopeStack.Value = newStack;

        return new LoggingScope(() => ScopeStack.Value = previous);
    }
}