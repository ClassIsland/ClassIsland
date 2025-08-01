using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ClassIsland.Core;
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
        var storageProvider = AppBase.Current.MainWindow?.StorageProvider;

        if (storageProvider == null)
        {
            return;
        }

        // 启动异步操作以打开对话框。
        if (!IsFolder)
        {
            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(StartFolder),
                FileTypeFilter = FileTypes.AsReadOnly(),
                AllowMultiple = false,
                SuggestedFileName = CurrentPath
            });

            if (files.Count > 0)
            {
                CurrentPath = files[0].TryGetLocalPath() ?? "";
            }
        }
        else
        {
            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(StartFolder),
                AllowMultiple = false,
                SuggestedFileName = CurrentPath
            });

            if (folders.Count > 0)
            {
                CurrentPath = folders[0].TryGetLocalPath() ?? "";
            }
        }
        FileSelected?.Invoke(this, EventArgs.Empty);
    }
}