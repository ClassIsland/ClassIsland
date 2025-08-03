using ClassIsland.Platforms.Abstraction.Services;
using Microsoft.Win32;

namespace ClassIsland.Platform.Windows.Services;

public class SystemEventsService : ISystemEventsService
{
    public event EventHandler? TimeChanged;

    public SystemEventsService()
    {
        
        SystemEvents.TimeChanged += (sender, args) => TimeChanged?.Invoke(sender, args);
    }
    
}