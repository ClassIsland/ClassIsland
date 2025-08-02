using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;

namespace ClassIsland.Platforms.Abstraction.Stubs.Services;

/// <inheritdoc />
public class DesktopToastServiceStub : IDesktopToastService
{
    /// <inheritdoc />
    public async Task ShowToastAsync(DesktopToastContent content)
    {
    }

    /// <inheritdoc />
    public async Task ShowToastAsync(string title, string body, Action? activated = null)
    {
    }

    /// <inheritdoc />
    public void ActivateNotificationAction(Guid id)
    {
    }
}