using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared.Interfaces.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

public static class NotificationEffectRegistryExtensions
{
    public static IServiceCollection AddNotificationEffect(this IServiceCollection services, string id, string name,
        Func<NotificationEffectContext, INotificationEffectControl?> factory)
    {
        if (INotificationWorkerService.RegisteredEffects.Any(x => x.Id == id))
        {
            throw new InvalidOperationException($"已存在 ID 为 {id} 的提醒特效。");
        }
        
        INotificationWorkerService.RegisteredEffects.Add(new NotificationEffectRegisterInfo()
        {
            Id = id,
            Name = name,
            Factory = factory
        });
        return services;
    }
}