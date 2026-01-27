using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models;

public partial class WeekClassPlanRow : ObservableObject
{
    [ObservableProperty] private TimeLayoutItem _timePoint = TimeLayoutItem.Empty;
    [ObservableProperty] private ClassInfo _monday = ClassInfo.Empty;
    [ObservableProperty] private ClassInfo _tuesday = ClassInfo.Empty;
    [ObservableProperty] private ClassInfo _wednesday = ClassInfo.Empty;
    [ObservableProperty] private ClassInfo _thursday = ClassInfo.Empty;
    [ObservableProperty] private ClassInfo _friday = ClassInfo.Empty;
    [ObservableProperty] private ClassInfo _saturday = ClassInfo.Empty;
    [ObservableProperty] private ClassInfo _sunday = ClassInfo.Empty;
}