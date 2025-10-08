﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ClassIsland.Core;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Platforms.Abstraction;
using Microsoft.Win32;

namespace ClassIsland.Controls;

public class FileBrowserButton : Button
{
    public static readonly StyledProperty<IList<FilePickerFileType>> FileTypesProperty = AvaloniaProperty.Register<FileBrowserButton, IList<FilePickerFileType>>(
        nameof(FileTypes), []);

    public IList<FilePickerFileType> FileTypes
    {
        get => GetValue(FileTypesProperty);
        set => SetValue(FileTypesProperty, value);
    }

    public static readonly StyledProperty<string> CurrentPathProperty = AvaloniaProperty.Register<FileBrowserButton, string>(
        nameof(CurrentPath));

    public string CurrentPath
    {
        get => GetValue(CurrentPathProperty);
        set => SetValue(CurrentPathProperty, value);
    }

    public static readonly StyledProperty<string> StartFolderProperty = AvaloniaProperty.Register<FileBrowserButton, string>(
        nameof(StartFolder));

    public string StartFolder
    {
        get => GetValue(StartFolderProperty);
        set => SetValue(StartFolderProperty, value);
    }

    public static readonly StyledProperty<bool> IsFolderProperty =
        AvaloniaProperty.Register<FileBrowserButton, bool>(
            nameof(IsFolder));

    public bool IsFolder
    {
        get => GetValue(IsFolderProperty);
        set => SetValue(IsFolderProperty, value);
    }

    public event EventHandler? FileSelected;

    protected override Type StyleKeyOverride => typeof(Button);


    static FileBrowserButton() { }

    protected async override void OnClick()
    {
        base.OnClick();
        var storageProvider = AppBase.Current.GetRootWindow().StorageProvider;

        // 启动异步操作以打开对话框。
        if (!IsFolder)
        {
            PopupHelper.DisableAllPopups();
            var files = await PlatformServices.FilePickerService.OpenFilesPickerAsync(new FilePickerOpenOptions
            {
                SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(StartFolder),
                FileTypeFilter = FileTypes.AsReadOnly(),
                AllowMultiple = false,
                SuggestedFileName = CurrentPath
            }, TopLevel.GetTopLevel(this) ?? AppBase.Current.GetRootWindow());
            PopupHelper.RestoreAllPopups();

            if (files.Count > 0)
            {
                CurrentPath = files[0];
            }
        }
        else
        {
            PopupHelper.DisableAllPopups();
            var folders = await PlatformServices.FilePickerService.OpenFoldersPickerAsync(new FolderPickerOpenOptions()
            {
                SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(StartFolder),
                AllowMultiple = false,
                SuggestedFileName = CurrentPath
            }, TopLevel.GetTopLevel(this) ?? AppBase.Current.GetRootWindow());
            PopupHelper.RestoreAllPopups();

            if (folders.Count > 0)
            {
                CurrentPath = folders[0];
            }
        }
        FileSelected?.Invoke(this, EventArgs.Empty);
    }




    public static readonly FilePickerFileType TypeApplication = new("应用程序")
    {
        Patterns =
            OperatingSystem.IsWindows() ? ["*.exe", "*.bat", "*.cmd"] :
            OperatingSystem.IsMacOS() ? ["*.app", "*.command", "*.sh"] :
            OperatingSystem.IsLinux() ? ["*.bin", "*.run", "*.sh", "*.deb", "*.rpm"] :
            null,
        AppleUniformTypeIdentifiers = ["com.apple.application", "public.executable", "public.shell-script"],
        MimeTypes = ["application/exe", "application/x-executable", "application/x-msdownload", "text/x-shellscript"]
    };

    public static readonly FilePickerFileType TypeAll = new("所有文件")
    {
        Patterns = ["*.*"],
        AppleUniformTypeIdentifiers = ["public.item"],
        MimeTypes = ["*/*"]
    };
}