using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Android.Services.Platform;

public class DesktopToastService : IDesktopToastService
{
    public Task ShowToastAsync(DesktopToastContent content)
    {
        throw new NotImplementedException();
    }

    public Task ShowToastAsync(string title, string body, Action? activated = null)
    {
        throw new NotImplementedException();
    }

    public void ActivateNotificationAction(Guid id)
    {
        throw new NotImplementedException();
    }
}