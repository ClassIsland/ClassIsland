using Microsoft.Extensions.Hosting;

namespace ClassIsland.Core;

public interface IAppHost
{
    public static Version CoreVersion = new Version(1, 4, 0, 0);

    public static IHost? Host;

    public static T GetService<T>()
    {
        var s = Host?.Services.GetService(typeof(T));
        if (s != null)
        {
            return (T)s;
        }

        throw new ArgumentException($"Service {typeof(T)} is null!");
    }

    public static T? TryGetService<T>()
    {
        return (T?)Host?.Services.GetService(typeof(T));
    }
}