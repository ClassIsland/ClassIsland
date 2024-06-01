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

    public bool DisableProfileClassPlanEditing
    {
        get => _disableProfileClassPlanEditing;
        set => SetProperty(ref _disableProfileClassPlanEditing, value);
    }

    public bool DisableProfileTimeLayoutEditing
    {
        get => _disableProfileTimeLayoutEditing;
        set => SetProperty(ref _disableProfileTimeLayoutEditing, value);
    }

    public bool DisableProfileSubjectsEditing
    {
        get => _disableProfileSubjectsEditing;
        set => SetProperty(ref _disableProfileSubjectsEditing, value);
    }

    public bool DisableProfileEditing
    {
        get => _disableProfileEditing;
        set => SetProperty(ref _disableProfileEditing, value);
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
}