using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
namespace ClassIsland.Services.NotificationProviders;

[NotificationProviderInfo("4B12F124-8585-43C7-AFC5-7BBB7CBE60D6", "行动提醒", "\ue01e", "显示由行动发出的提醒。")]
public class ActionNotificationProvider : NotificationProviderBase
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }
}