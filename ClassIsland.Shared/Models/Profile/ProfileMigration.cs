using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 表示档案的一次迁移的信息
/// </summary>
public partial class ProfileMigration : ObservableObject
{
    [ObservableProperty] private string _id = "";
    [ObservableProperty] private bool _allowDowngrade = true;
    [ObservableProperty] private bool _removeOnDowngrade = false;
}