using System;

namespace ClassIsland.Controls;

internal interface INotificationRenderSizeHost
{
    // Height is in DIPs after applying the user's layout scale.
    IDisposable ReserveRenderHeight(double additionalHeight);
}
