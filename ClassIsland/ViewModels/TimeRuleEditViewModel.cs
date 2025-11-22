using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.ViewModels;

public partial class TimeRuleEditViewModel : ObservableObject
{
    public static IReadOnlyList<string> WeekDayOptions { get; } =
        ["日", "一", "二", "三", "四", "五", "六"];

    public List<string> WeekCountDivOptions { get; set; } = [];
    
    public List<string> WeekCountDivTotalOptions { get; set; } = [];
    
    [ObservableProperty]
    private int _weekCountDivIndex;
    
    [ObservableProperty]
    private int _weekCountDivTotalIndex;
}