using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public partial class WeekClassPlanRow : ObservableObject
{
    [ObservableProperty] private ClassInfo? _monday;
    [ObservableProperty] private ClassInfo? _tuesday;
    [ObservableProperty] private ClassInfo? _wednesday;
    [ObservableProperty] private ClassInfo? _thursday;
    [ObservableProperty] private ClassInfo? _friday;
    [ObservableProperty] private ClassInfo? _saturday;
    [ObservableProperty] private ClassInfo? _sunday;
}