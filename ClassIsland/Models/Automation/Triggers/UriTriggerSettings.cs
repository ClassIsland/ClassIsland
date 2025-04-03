using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Automation.Triggers;

public partial class UriTriggerSettings : ObservableObject
{
    [ObservableProperty] private string _uriSuffix = "";
}