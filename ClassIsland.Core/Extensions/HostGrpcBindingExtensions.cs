using ClassIsland.Core.Helpers;
using Grpc.Core.Logging;
using GrpcDotNetNamedPipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Core.Extensions;

/// <summary>
/// <see cref="IHost"/>主机 Grpc 绑定扩展
/// </summary>
public static class HostGrpcBindingExtensions
{
    private static List<Type> GrpcServices { get; } = new();

    public static IServiceCollection AddGrpcService<T>(this IServiceCollection services) where T : class
    {
        if (GrpcServices.Contains(typeof(T)))
        {
            throw new InvalidOperationException($"已注册 Grpc 服务： {typeof(T)}");
        }
        services.AddSingleton<T>();
        GrpcServices.Add(typeof(T));
        return services;
    }

    internal static IHost BindGrpcServices(this IHost host)
    {
        var logger = host.Services.GetService(typeof(ILogger<IHost>)) as ILogger<IHost>;
        ArgumentNullException.ThrowIfNull(host);
        foreach (var s in GrpcServices)
        {
            var method = BindMethodFinder.GetBindMethod(s);
            var server = host.Services.GetService(typeof(NamedPipeServer)) as NamedPipeServer;
            if (method == null) continue;
            method.Invoke(null, [server?.ServiceBinder, host.Services.GetRequiredService(s)]);
            logger?.LogInformation("绑定 Grpc 服务：{}", s);
        }

        return host;
    }
}