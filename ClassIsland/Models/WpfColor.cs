using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public partial class WpfColor : ObservableObject
{
    [ObservableProperty] private byte _a;
    [ObservableProperty] private byte _r;
    [ObservableProperty] private byte _g;
    [ObservableProperty] private byte _b;
}