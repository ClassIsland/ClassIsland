using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class TimeRuleEditViewModel : ObservableObject
{
    public static IReadOnlyList<string> WeekDayOptions { get; } =
        ["周日", "周一", "周二", "周三", "周四", "周五", "周六"];

    public List<string> WeekCountDivOptions { get; set; } = [];
    
    public List<string> WeekCountDivTotalOptions { get; set; } = [];
    
    [ObservableProperty]
    private int _weekCountDivIndex;
    
    [ObservableProperty]
    private int _weekCountDivTotalIndex;
}