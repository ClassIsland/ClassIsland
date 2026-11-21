using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Models.Actions;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;
using static ClassIsland.Models.Actions.RunActionSettings.RunActionRunType;
namespace ClassIsland.Controls.ActionSettingsControls;

public partial class RunActionSettingsControl : ActionSettingsControlBase<RunActionSettings>, INotifyPropertyChanged
{
    public bool IsAppleMobile => PlatformHelper.IsAppleMobile;

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
        Hint = "";
        switch (Settings.RunType)
        {
            case RunActionSettings.RunActionRunType.Application:
                IsApplication = IsFileSelector = !IsAppleMobile;
                FileTypes = [FileBrowserButton.TypeApplication, FileBrowserButton.TypeAll];
                Watermark = "应用程序";
                ChangeActionName("运行");
                if (IsAppleMobile)
                    Hint = "此行动不能在 iOS 上运行。请改选“App 链接”，并填写目标 App 提供的链接。";
                break;
            case File:
                IsFileSelector = true;
                FileTypes = [FileBrowserButton.TypeAll];
                Watermark = "文件";
                ChangeActionName("打开");
                if (IsAppleMobile)
                    Hint = "选择的文件会保存到 ClassIsland，运行时使用系统预览打开。重新打开应用后仍可使用。";
                break;
            case Folder:
                IsFileSelector = IsFolder = true;
                Watermark = "文件夹";
                ChangeActionName("打开");
                if (IsAppleMobile)
                    Hint = "选择的文件夹会复制到 ClassIsland，运行时在“文件”App 中打开该副本。";
                break;
            case Url:
                Watermark = "Url 链接";
                ChangeActionName("打开");
                if (IsAppleMobile)
                    Hint = "打开网页链接；若要打开其他 App，请选择“App 链接”。";
                break;
            case AppLink:
                Watermark = "目标 App 提供的完整链接";
                ChangeActionName("打开");
                Hint = "填写目标 App 提供的链接，需先安装该 App。网页形式的链接也可能在浏览器中打开。";
                break;
            case Command:
                Watermark = OperatingSystem.IsWindows() ? "cmd 命令" : "终端命令";
                ChangeActionName("运行");
                if (IsAppleMobile)
                    Hint = "iOS 不支持运行终端命令。请改选其他行动类型，或删除此行动。";
                break;
            default:
                Watermark = "";
                break;
        }
    }

    async void FileSelectorButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PopupHelper.DisableAllPopups();
        try
        {
            var root = TopLevel.GetTopLevel(this) ?? AppBase.Current.GetRootWindow();
            var path = ImportedFileReference.Resolve(Settings.Value);
            var folderPath = IsFolder ? path : System.IO.Path.GetDirectoryName(path);
            using var startLocation = string.IsNullOrWhiteSpace(folderPath)
                ? null
                : await root.StorageProvider.TryGetFolderFromPathAsync(folderPath);
            var items = !IsFolder
                ? await PlatformServices.FilePickerService.OpenPersistentFilesPickerAsync(new()
                {
                    SuggestedStartLocation = startLocation,
                    FileTypeFilter = FileTypes,
                    AllowMultiple = false,
                    SuggestedFileName = System.IO.Path.GetFileName(path)
                }, root)
                : await PlatformServices.FilePickerService.OpenPersistentFoldersPickerAsync(new()
                {
                    SuggestedStartLocation = startLocation,
                    AllowMultiple = false
                }, root);
            if (items.Count > 0)
            {
                Settings.Value = items[0];
            }
        }
        catch (Exception exception)
        {
            IAppHost.TryGetService<ILogger<RunActionSettingsControl>>()?
                .LogError(exception, "Failed to select an automation file or folder.");
            ToastsHelper.ShowErrorToast(this, "无法保存所选内容，请确认文件可以访问、存储空间充足后重试。");
        }
        finally
        {
            PopupHelper.RestoreAllPopups();
        }
    }

    string _hint = "";
    public string Hint
    {
        get => _hint;
        set
        {
            if (value == _hint) return;
            _hint = value;
            OnPropertyChanged();
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
