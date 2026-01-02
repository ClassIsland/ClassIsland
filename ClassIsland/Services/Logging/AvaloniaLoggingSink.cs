using System;
using System.Text;
using Avalonia;
using Avalonia.Logging;
using Avalonia.Utilities;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.Logging;

public class AvaloniaLoggingSink(LogEventLevel level) : ILogSink
{
    private LogEventLevel Level { get; } = level;
    
    ILogger<AvaloniaLoggingSink>? _logger;

    ILogger<AvaloniaLoggingSink>? Logger
    {
        get
        {
            if (_logger != null)
            {
                return _logger;
            }

            return _logger = IAppHost.TryGetService<ILogger<AvaloniaLoggingSink>>();
        }
    }

    public bool IsEnabled(LogEventLevel level, string area)
    {
        if (area == LogArea.Binding && level == LogEventLevel.Warning)
            return false;

        return level >= Level;
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        if (area == LogArea.Binding && level == LogEventLevel.Warning)
            return;
        if (Logger == null)
        {
            return;
        }
        
        var message = Format<object, object, object>(area, messageTemplate, source, null);

        switch (level)
        {
            case LogEventLevel.Verbose:
                Logger.LogTrace("{}", message);
                break;
            case LogEventLevel.Debug:
                Logger.LogDebug("{}", message);
                break;
            case LogEventLevel.Information:
                Logger.LogInformation("{}", message);
                break;
            case LogEventLevel.Warning:
                Logger.LogWarning("{}", message);
                break;
            case LogEventLevel.Error:
                Logger.LogError("{}", message);
                break;
            case LogEventLevel.Fatal:
                Logger.LogCritical("{}", message);
                break;
            default:
                Logger.LogInformation("{}", message);
                break;
        }
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate,
        params object?[] propertyValues)
    {
        if (Logger == null)
        {
            return;
        }
        
        var message = Format<object, object, object>(area, messageTemplate, source, propertyValues);

        switch (level)
        {
            case LogEventLevel.Verbose:
                Logger.LogTrace("{}", message);
                break;
            case LogEventLevel.Debug:
                Logger.LogDebug("{}", message);
                break;
            case LogEventLevel.Information:
                Logger.LogInformation("{}", message);
                break;
            case LogEventLevel.Warning:
                Logger.LogWarning("{}", message);
                break;
            case LogEventLevel.Error:
                Logger.LogError("{}", message);
                break;
            case LogEventLevel.Fatal:
                Logger.LogCritical("{}", message);
                break;
            default:
                Logger.LogInformation("{}", message);
                break;
        }
    }


    private static string Format<T0, T1, T2>(
        string area,
        string template,
        object? source,
        object?[]? values)
    {
        var result = new StringBuilder();
        var r = new CharacterReader(template.AsSpan());
        var i = 0;

        result.Append('[');
        result.Append(area);
        result.Append("] ");

        while (!r.End)
        {
            var c = r.Take();

            if (c != '{')
            {
                result.Append(c);
            }
            else
            {
                if (r.Peek != '{')
                {
                    result.Append('\'');
                    result.Append(values?[i++]);
                    result.Append('\'');
                    r.TakeUntil('}');
                    r.Take();
                }
                else
                {
                    result.Append('{');
                    r.Take();
                }
            }
        }

        FormatSource(source, result);
        return result.ToString();
    }

    private static void FormatSource(object? source, StringBuilder result)
    {
        if (source is null)
            return;

        result.Append(" (");
        result.Append(source.GetType().Name);
        result.Append(" #");

        if (source is StyledElement se && se.Name is not null)
            result.Append(se.Name);
        else
            result.Append(source.GetHashCode());

        result.Append(')');
    }
}
