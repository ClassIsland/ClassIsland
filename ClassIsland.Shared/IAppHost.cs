using Microsoft.Extensions.Hosting;

namespace ClassIsland.Shared;

/// <summary>
/// 应用主机接口
/// </summary>
public interface IAppHost
{
    /// <summary>
    /// 核心库版本
    /// </summary>
    public static Version CoreVersion = new Version(1, 4, 0, 0);

    /// <summary>
    /// 应用主机
    /// </summary>
    public static IHost? Host;

    /// <summary>
    /// 获取指定的服务
    /// </summary>
    /// <typeparam name="T">要获取的服务类型</typeparam>
    /// <returns>获取的服务</returns>
    /// <exception cref="ArgumentException"></exception>
    public static T GetService<T>()
    {
        var s = Host?.Services.GetService(typeof(T));
        if (s != null)
        {
            return (T)s;
        }

        throw new ArgumentException($"Service {typeof(T)} is null!");
    }

    /// <summary>
    /// 尝试获取指定的服务
    /// </summary>
    /// <typeparam name="T">要获取的服务类型</typeparam>
    /// <returns>如果获取成功，则返回获取到的服务，否则返回null</returns>
    public static T? TryGetService<T>()
    {
        return (T?)Host?.Services.GetService(typeof(T));
    }
}