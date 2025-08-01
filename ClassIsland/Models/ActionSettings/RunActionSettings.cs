using ClassIsland.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.ActionSettings;

/// <summary>
/// "运行"行动设置
/// </summary>
public partial class RunActionSettings : ObservableRecipient
{
    [ObservableProperty]
    RunActionRunType _runType;

    [ObservableProperty]
    string _value = "";

    [ObservableProperty]
    string _args = "";
}