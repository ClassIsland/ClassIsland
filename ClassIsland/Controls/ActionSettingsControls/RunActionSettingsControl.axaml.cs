using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Enums;
using ClassIsland.Models.ActionSettings;
namespace ClassIsland.Controls.ActionSettingsControls;

public sealed partial class RunActionSettingsControl : ActionSettingsControlBase<RunActionSettings>, INotifyPropertyChanged
{
    public RunActionSettingsControl()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Settings.PropertyChanged += SettingsOnPropertyChanged;
            OnRunTypeChanged();
        };
        Unloaded += (_, _) => Settings.PropertyChanged -= SettingsOnPropertyChanged;
    }

    void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.RunType))
        {
            OnRunTypeChanged();
        }
    }

    void OnRunTypeChanged()
    {
        IsApplication = IsFileSelector = IsFolder = false;
        FileTypes = "";
        switch (Settings.RunType)
        {
            case RunActionRunType.Application:
                IsApplication = IsFileSelector = true;
                FileTypes = "";
                Watermark = "应用程序";
                break;
            case RunActionRunType.File:
                IsFileSelector = true;
                FileTypes = "";
                Watermark = "文件";
                break;
            case RunActionRunType.Folder:
                IsFileSelector = IsFolder = true;
                Watermark = "文件夹";
                break;
            case RunActionRunType.Url:
                Watermark = "Url 链接";
                break;
            case RunActionRunType.Command:
                Watermark =
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                        "cmd 命令" : "终端命令";
                break;
            default:
                Watermark = "";
                break;
        }
    }

    string _watermark = "";
    public string Watermark
    {
        get => _watermark;
        set
        {
            if (value == _watermark) return;
            _watermark = value;
            OnPropertyChanged();
        }
    }

    bool _isApplication;
    public bool IsApplication
    {
        get => _isApplication;
        set
        {
            if (value == _isApplication) return;
            _isApplication = value;
            OnPropertyChanged();
        }
    }

    bool _isFileSelector;
    public bool IsFileSelector
    {
        get => _isFileSelector;
        set
        {
            if (value == _isFileSelector) return;
            _isFileSelector = value;
            OnPropertyChanged();
        }
    }

    bool _isFolder;
    public bool IsFolder
    {
        get => _isFolder;
        set
        {
            if (value == _isFolder) return;
            _isFolder = value;
            OnPropertyChanged();
        }
    }

    string _fileTypes = "";
    public string FileTypes
    {
        get => _fileTypes;
        set
        {
            if (value == _fileTypes) return;
            _fileTypes = value;
            OnPropertyChanged();
        }
    }

    public string TerminalCommandText { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd 命令" : "终端命令";

#region PropertyChanged
    public new event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
#endregion
}
