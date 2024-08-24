using ClassIsland.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Management;

public class ManagementSettings : ObservableRecipient
{
    private ManagementServerKind _managementServerKind = ManagementServerKind.Serverless;
    private string _managementServer = "";
    private string _manifestUrlTemplate = "";
    private string? _classIdentity = "";
    private bool _isManagementEnabled = false;
    private string _managementServerGrpc = "";

    /// <summary>
    /// 是否启用集控
    /// </summary>
    public bool IsManagementEnabled
    {
        get => _isManagementEnabled;
        set
        {
            if (value == _isManagementEnabled) return;
            _isManagementEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 管理服务器类型
    /// </summary>
    public ManagementServerKind ManagementServerKind
    {
        get => _managementServerKind;
        set
        {
            if (value == _managementServerKind) return;
            _managementServerKind = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 集控服务器地址，此条目仅在ManagementServerKind为ManagementServer时起作用。
    /// </summary>
    public string ManagementServer
    {
        get => _managementServer;
        set
        {
            if (value == _managementServer) return;
            _managementServer = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 集控服务器Grpc地址，此条目仅在ManagementServerKind为ManagementServer时起作用。
    /// </summary>
    public string ManagementServerGrpc
    {
        get => _managementServerGrpc;
        set
        {
            if (value == _managementServerGrpc) return;
            _managementServerGrpc = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 管理清单url模板，此条目仅在ManagementServerKind为Serverless时起作用。
    /// </summary>
    public string ManifestUrlTemplate
    {
        get => _manifestUrlTemplate;
        set
        {
            if (value == _manifestUrlTemplate) return;
            _manifestUrlTemplate = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 班级标识符，可选。
    /// </summary>
    public string? ClassIdentity
    {
        get => _classIdentity;
        set
        {
            if (value == _classIdentity) return;
            _classIdentity = value;
            OnPropertyChanged();
        }
    }
}