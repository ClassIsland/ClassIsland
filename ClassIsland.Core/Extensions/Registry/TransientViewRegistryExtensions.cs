using System.Collections.Concurrent;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Services.UI;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

public static class TransientViewRegistryExtensions
{
    public static IServiceCollection AddTransientView<TView>(this IServiceCollection services) where TView : ViewBase
    {
        services.AddTransient<TView>(sp => ViewManagementService.Instance.GetOrCreateTransientView<TView>(sp));
        return services;
    }
}