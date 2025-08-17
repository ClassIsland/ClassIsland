using System.Linq;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.Actions;
using ClassIsland.Services.NotificationProviders;
using ClassIsland.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace ClassIsland.Services.Automation.Actions;

[ActionInfo("classisland.notification.weather", "显示天气提醒", "\uf44f", addDefaultToMenu:false)]
public class WeatherNotificationAction : ActionBase<WeatherNotificationActionSettings>
{
    static WeatherNotificationProvider WeatherNotificationProvider { get; } =
        IAppHost.Host.Services.GetServices<IHostedService>().OfType<WeatherNotificationProvider>().First();

    protected override async Task OnInvoke()
    {
        switch (Settings.NotificationKind)
        {
            case 0:
                WeatherNotificationProvider.ShowWeatherForecastCore();
                break;
            case 1:
                WeatherNotificationProvider.ShowAlertsNotificationCore();
                break;
            case 2:
                WeatherNotificationProvider.ShowWeatherForecastHourlyCore();
                break;
        }
    }
}