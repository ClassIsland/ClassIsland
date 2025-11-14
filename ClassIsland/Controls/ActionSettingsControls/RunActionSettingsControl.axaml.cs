using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Models.Actions;
using ClassIsland.Platforms.Abstraction;
using static ClassIsland.Models.Actions.RunActionSettings.RunActionRunType;
namespace ClassIsland.Controls.ActionSettingsControls;

public partial class RunActionSettingsControl : ActionSettingsControlBase<RunActionSettings>, INotifyPropertyChanged
{
    public RunActionSettingsControl()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Settings.PropertyChanged += SettingsOnPropertyChanged;
        OnRunTypeChanged();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Settings.PropertyChanged -= SettingsOnPropertyChanged;
    }

    void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.RunType)) OnRunTypeChanged();
    }

    void OnRunTypeChanged()
    {
        IsApplication = IsFileSelector = IsFolder = false;
        FileTypes = null;
        switch (Settings.RunType)
        {
            case RunActionSettings.RunActionRunType.Application:
                IsApplication = IsFileSelector = true;
                FileTypes = [FileBrowserButton.TypeApplication, FileBrowserButton.TypeAll];
                Watermark = "应用程序";
                ChangeActionName("运行");
                break;
            case File:
                IsFileSelector = true;
                FileTypes = [FileBrowserButton.TypeAll];
                Watermark = "文件";
                ChangeActionName("打开");
                break;
            case Folder:
                IsFileSelector = IsFolder = true;
                Watermark = "文件夹";
                ChangeActionName("打开");
                break;
            case Url:
                Watermark = "Url 链接";
                ChangeActionName("打开");
                break;
            case Command:
                Watermark = OperatingSystem.IsWindows() ? "cmd 命令" : "终端命令";
                ChangeActionName("运行");
                break;
            default:
                Watermark = "";
                break;
        }
    }

    async void FileSelectorButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var storageProvider = AppBase.Current.GetRootWindow().StorageProvider;

        // 启动异步操作以打开对话框。
        if (!IsFolder)
        {
            PopupHelper.DisableAllPopups();
            var files = await PlatformServices.FilePickerService.OpenFilesPickerAsync(new()
            {
                SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(Settings.Value),
                FileTypeFilter = FileTypes,
                SuggestedFileName = Settings.Value,
            }, TopLevel.GetTopLevel(this) ?? AppBase.Current.GetRootWindow());
            PopupHelper.RestoreAllPopups();

            if (files.Count > 0)
            {
                Settings.Value = files[0];
            }
        }
        else
        {
            PopupHelper.DisableAllPopups();
            var folders = await PlatformServices.FilePickerService.OpenFilesPickerAsync(new()
            {
                SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(Settings.Value),
                SuggestedFileName = Settings.Value
            }, TopLevel.GetTopLevel(this) ?? AppBase.Current.GetRootWindow());
            PopupHelper.RestoreAllPopups();

            if (folders.Count > 0)
            {
                Settings.Value = folders[0];
            }
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

    List<FilePickerFileType>? FileTypes { get; set; } = [];

#region PropertyChanged
    public new event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
#endregion
}
