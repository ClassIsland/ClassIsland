using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <summary>
/// 启动器服务桩服务
/// </summary>
public class LauncherServiceStub : ILauncherService
{
    /// <inheritdoc />
    public Task LaunchPath(string path)
    {
        return Task.CompletedTask;
    }

    public Task LaunchUrl(string url)
    {
        return Task.CompletedTask;
    }
}