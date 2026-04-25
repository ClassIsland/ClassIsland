using System;
using System.Text;
using Avalonia;
using Avalonia.Logging;
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
        return level >= Level;
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
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
        var i = 0;
        var pos = 0;

        result.Append('[');
        result.Append(area);
        result.Append("] ");

        while (pos < template.Length)
        {
            var c = template[pos++];

            if (c != '{')
            {
                result.Append(c);
            }
            else
            {
                if (pos < template.Length && template[pos] == '{')
                {
                    // Escaped '{{' → literal '{'
                    result.Append('{');
                    pos++;
                }
                else
                {
                    // Placeholder: consume until '}'
                    result.Append('\'');
                    if (values != null && i < values.Length)
                        result.Append(values[i]);
                    i++;
                    result.Append('\'');

                    while (pos < template.Length && template[pos] != '}')
                        pos++;
                    if (pos < template.Length)
                        pos++; // skip '}'
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
