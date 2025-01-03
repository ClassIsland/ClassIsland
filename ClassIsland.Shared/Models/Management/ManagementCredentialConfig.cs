using ClassIsland.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Management;

/// <summary>
/// 代表一个集控认证配置。
/// </summary>
public class ManagementCredentialConfig : ObservableRecipient
{
    private string _userCredential = "";
    private string _adminCredential = "";
    private AuthorizeLevel _editAuthorizeSettingsAuthorizeLevel = AuthorizeLevel.None;
    private AuthorizeLevel _exitManagementAuthorizeLevel = AuthorizeLevel.None;
    private AuthorizeLevel _editProfileAuthorizeLevel = AuthorizeLevel.None;
    private AuthorizeLevel _editSettingsAuthorizeLevel = AuthorizeLevel.None;
    private AuthorizeLevel _exitApplicationAuthorizeLevel = AuthorizeLevel.None;
    private AuthorizeLevel _changeLessonsAuthorizeLevel = AuthorizeLevel.None;
    private AuthorizeLevel _editPolicyAuthorizeLevel = AuthorizeLevel.None;

    /// <summary>
    /// 用户凭据
    /// </summary>
    public string UserCredential
    {
        get => _userCredential;
        set => SetProperty(ref _userCredential, value);
    }

    /// <summary>
    /// 管理员凭据
    /// </summary>
    public string AdminCredential
    {
        get => _adminCredential;
        set => SetProperty(ref _adminCredential, value);
    }

    /// <summary>
    /// 编辑授权设置的授权等级
    /// </summary>
    public AuthorizeLevel EditAuthorizeSettingsAuthorizeLevel
    {
        get => _editAuthorizeSettingsAuthorizeLevel;
        set => SetProperty(ref _editAuthorizeSettingsAuthorizeLevel, value);
    }

    /// <summary>
    /// 编辑策略的授权等级
    /// </summary>
    public AuthorizeLevel EditPolicyAuthorizeLevel
    {
        get => _editPolicyAuthorizeLevel;
        set => SetProperty(ref _editPolicyAuthorizeLevel, value);
    }

    /// <summary>
    /// 退出集控的授权等级
    /// </summary>
    public AuthorizeLevel ExitManagementAuthorizeLevel
    {
        get => _exitManagementAuthorizeLevel;
        set => SetProperty(ref _exitManagementAuthorizeLevel, value);
    }

    /// <summary>
    /// 编辑档案的授权等级
    /// </summary>

    public AuthorizeLevel EditProfileAuthorizeLevel
    {
        get => _editProfileAuthorizeLevel;
        set => SetProperty(ref _editProfileAuthorizeLevel, value);
    }

    /// <summary>
    /// 编辑设置的授权等级
    /// </summary>
    public AuthorizeLevel EditSettingsAuthorizeLevel
    {
        get => _editSettingsAuthorizeLevel;
        set => SetProperty(ref _editSettingsAuthorizeLevel, value);
    }

    /// <summary>
    /// 退出应用的授权等级
    /// </summary>
    public AuthorizeLevel ExitApplicationAuthorizeLevel
    {
        get => _exitApplicationAuthorizeLevel;
        set => SetProperty(ref _exitApplicationAuthorizeLevel, value);
    }

    /// <summary>
    /// 换课的授权等级
    /// </summary>
    public AuthorizeLevel ChangeLessonsAuthorizeLevel
    {
        get => _changeLessonsAuthorizeLevel;
        set => SetProperty(ref _changeLessonsAuthorizeLevel, value);
    }
}