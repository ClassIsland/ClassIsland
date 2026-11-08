using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ClassIsland.Shared.Models.Management;

/// <summary>
/// 限制策略
/// </summary>
public class ManagementPolicy : ObservableRecipient
{
    private bool _disableProfileClassPlanEditing = false;
    private bool _disableProfileTimeLayoutEditing = false;
    private bool _disableProfileSubjectsEditing = false;
    private bool _disableProfileEditing = false;
    private bool _disableSettingsEditing = false;
    private bool _disableSplashCustomize = false;
    private bool _disableDebugMenu = false;
    private bool _allowExitManagement = true;
    private bool _disableEasterEggs = false;

    /// <summary>
    /// 是否允许使用需要完整档案编辑权限的功能。
    /// </summary>
    [JsonIgnore]
    public bool CanEditProfile =>
        !DisableProfileEditing &&
        !DisableProfileClassPlanEditing &&
        !DisableProfileTimeLayoutEditing &&
        !DisableProfileSubjectsEditing;

    public bool DisableProfileClassPlanEditing
    {
        get => _disableProfileClassPlanEditing;
        set
        {
            if (SetProperty(ref _disableProfileClassPlanEditing, value))
            {
                OnPropertyChanged(nameof(CanEditProfile));
            }
        }
    }

    public bool DisableProfileTimeLayoutEditing
    {
        get => _disableProfileTimeLayoutEditing;
        set
        {
            if (SetProperty(ref _disableProfileTimeLayoutEditing, value))
            {
                OnPropertyChanged(nameof(CanEditProfile));
            }
        }
    }

    public bool DisableProfileSubjectsEditing
    {
        get => _disableProfileSubjectsEditing;
        set
        {
            if (SetProperty(ref _disableProfileSubjectsEditing, value))
            {
                OnPropertyChanged(nameof(CanEditProfile));
            }
        }
    }

    public bool DisableProfileEditing
    {
        get => _disableProfileEditing;
        set
        {
            if (SetProperty(ref _disableProfileEditing, value))
            {
                OnPropertyChanged(nameof(CanEditProfile));
            }
        }
    }

    public bool DisableSettingsEditing
    {
        get => _disableSettingsEditing;
        set => SetProperty(ref _disableSettingsEditing, value);
    }

    public bool DisableSplashCustomize
    {
        get => _disableSplashCustomize;
        set => SetProperty(ref _disableSplashCustomize, value);
    }

    public bool DisableDebugMenu
    {
        get => _disableDebugMenu;
        set => SetProperty(ref _disableDebugMenu, value);
    }

    public bool AllowExitManagement
    {
        get => _allowExitManagement;
        set => SetProperty(ref _allowExitManagement, value);
    }

    public bool DisableEasterEggs
    {
        get => _disableEasterEggs;
        set => SetProperty(ref _disableEasterEggs, value);
    }
}
