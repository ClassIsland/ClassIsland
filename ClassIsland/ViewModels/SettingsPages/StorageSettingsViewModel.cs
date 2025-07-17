using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Services;
using ClassIsland.Views.SettingPages;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class StorageSettingsViewModel(
    FileFolderService fileFolderService,
    SettingsService settingsService,
    ILogger<StorageSettingsPage> logger,
    IManagementService managementService) : ObservableRecipient
{
    public FileFolderService FileFolderService { get; } = fileFolderService;
    public SettingsService SettingsService { get; } = settingsService;
    public ILogger<StorageSettingsPage> Logger { get; } = logger;
    public IManagementService ManagementService { get; } = managementService;
    
    [ObservableProperty] private bool _isBackingUp = false;
    [ObservableProperty] private bool _isBackupFinished = false;
    
}