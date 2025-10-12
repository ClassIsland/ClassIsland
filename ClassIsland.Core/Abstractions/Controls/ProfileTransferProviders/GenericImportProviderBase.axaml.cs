using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Shared;
using ClassIsland.Shared.Extensions;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Profile;
using CsesSharp;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Core.Abstractions.Controls.ProfileTransferProviders;

public abstract partial class GenericImportProviderBase : ProfileTransferProviderControlBase
{
    public static readonly StyledProperty<int> ImportTypeProperty = AvaloniaProperty.Register<GenericImportProviderBase, int>(
        nameof(ImportType));

    public int ImportType
    {
        get => GetValue(ImportTypeProperty);
        set => SetValue(ImportTypeProperty, value);
    }

    public static readonly StyledProperty<string> NewProfileNameProperty = AvaloniaProperty.Register<GenericImportProviderBase, string>(
        nameof(NewProfileName));

    public string NewProfileName
    {
        get => GetValue(NewProfileNameProperty);
        set => SetValue(NewProfileNameProperty, value);
    }

    public static readonly StyledProperty<string> SourceFilePathProperty = AvaloniaProperty.Register<GenericImportProviderBase, string>(
        nameof(SourceFilePath));

    public string SourceFilePath
    {
        get => GetValue(SourceFilePathProperty);
        set => SetValue(SourceFilePathProperty, value);
    }

    public static readonly StyledProperty<string> ImportFileHeaderProperty = AvaloniaProperty.Register<GenericImportProviderBase, string>(
        nameof(ImportFileHeader), "源文件路径");

    public string ImportFileHeader
    {
        get => GetValue(ImportFileHeaderProperty);
        set => SetValue(ImportFileHeaderProperty, value);
    }

    public static readonly StyledProperty<bool> AllowMergeToCurrentProfileProperty = AvaloniaProperty.Register<GenericImportProviderBase, bool>(
        nameof(AllowMergeToCurrentProfile), true);

    public bool AllowMergeToCurrentProfile
    {
        get => GetValue(AllowMergeToCurrentProfileProperty);
        set => SetValue(AllowMergeToCurrentProfileProperty, value);
    }

    public List<FilePickerFileType> FileTypes { get; init; } = [];

    private IProfileService ProfileService { get; } = IAppHost.GetService<IProfileService>();
    
    protected GenericImportProviderBase()
    {
        DataContext = this;
        InitializeComponent();
    }

    private async void ButtonPickFile_OnClick(object? sender, RoutedEventArgs e)
    {
        PopupHelper.DisableAllPopups();
        var files = await PlatformServices.FilePickerService.OpenFilesPickerAsync(new FilePickerOpenOptions
        {
            FileTypeFilter = FileTypes.AsReadOnly(),
            AllowMultiple = false,
            SuggestedFileName = SourceFilePath
        }, TopLevel.GetTopLevel(this) ?? AppBase.Current.GetRootWindow());
        PopupHelper.RestoreAllPopups();

        if (files.Count > 0)
        {
            SourceFilePath = files[0];
        }
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (!AllowMergeToCurrentProfile)
        {
            ImportType = 1;
        }
    }
}