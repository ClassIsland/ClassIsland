using Avalonia;
using Avalonia.Logging;
using ClassIsland.Services.Logging;

namespace ClassIsland.Extensions;

public static class AvaloniaLoggingSinkExtensions
{
    public static AppBuilder LogToHostSink(this AppBuilder builder, LogEventLevel level = LogEventLevel.Warning)
    {
        Logger.Sink = new AvaloniaLoggingSink(level);
        return builder;
    }
}