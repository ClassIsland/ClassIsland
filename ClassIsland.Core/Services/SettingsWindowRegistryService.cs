using System.Collections.ObjectModel;
using ClassIsland.Core.Attributes;

namespace ClassIsland.Core.Services;

public class SettingsWindowRegistryService
{
    public static ObservableCollection<SettingsPageInfo> Registered { get; } = new();
}