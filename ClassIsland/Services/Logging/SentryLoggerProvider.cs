using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.Logging;

public class SentryLoggerProvider : ILoggerProvider
{
    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SentryEventLogger(categoryName);
    }
}