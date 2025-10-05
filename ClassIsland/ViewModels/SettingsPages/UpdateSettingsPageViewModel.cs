using ClassIsland.Services;
using ClassIsland.Services.AppUpdating;
using ClassIsland.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhainonDistributionCenter.Shared.Models.Client;

namespace ClassIsland.ViewModels.SettingsPages;

public partial class UpdateSettingsPageViewModel(ILogger<UpdateSettingsPage> logger, UpdateService updateService, SettingsService settingsService) : ObservableObject
{
    public ILogger<UpdateSettingsPage> Logger { get; } = logger;
    public UpdateService UpdateService { get; } = updateService;
    public SettingsService SettingsService { get; } = settingsService;

    [ObservableProperty] private DistributionMetadata.DistributionChannel _selectedChannel = new();
}