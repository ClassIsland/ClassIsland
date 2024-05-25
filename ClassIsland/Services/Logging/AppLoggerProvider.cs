using Microsoft.Extensions.Logging;

namespace ClassIsland.Services.Logging;

public class AppLoggerProvider(AppLogService appLogService) : ILoggerProvider
{
    private AppLogService AppLogService { get; } = appLogService;

    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new AppLogger(AppLogService, categoryName);
    }
}