using System;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Services;
using ClassIsland.Shared.Models.Profile;

using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;

namespace ClassIsland.ViewModels;

public partial class ClassChangingViewModel : ObservableRecipient
{
    [ObservableProperty] private bool _writeToSourceClassPlan = false;
    [ObservableProperty] private int _slideIndex = 0;
    [ObservableProperty] private int _sourceIndex = -1;
    [ObservableProperty] private int _swapModeTargetIndex = -1;
    [ObservableProperty] private Subject? _targetSubject;
    [ObservableProperty] private bool _isAutoNextStep = false;
    [ObservableProperty] private Guid _targetSubjectIndex;
    [ObservableProperty] private int _classChangeMode;
    [ObservableProperty] private bool _canCompleteClassChanging = false;
    [ObservableProperty] private ClassInfo? _selectedClassInfo;
    [ObservableProperty] private TimeLayoutItem? _selectedTimeLayoutItem;
    
    public SettingsService SettingsService { get; }

    public IProfileService ProfileService { get; }

    public IManagementService ManagementService { get; }
    
    public SyncDictionaryList<Guid, Subject> Subjects { get; }

    public ClassChangingViewModel(IProfileService profileService, IManagementService managementService, SettingsService settingsService)
    {
        ProfileService = profileService;
        ManagementService = managementService;
        SettingsService = settingsService;

        Subjects = new SyncDictionaryList<Guid, Subject>(ProfileService.Profile.Subjects, Guid.NewGuid);
        this.ObservableForProperty(x => x.TargetSubjectIndex)
            .Subscribe(_ => UpdateCanCompleteClassChanging());
        this.ObservableForProperty(x => x.SwapModeTargetIndex)
            .Subscribe(_ => UpdateCanCompleteClassChanging());
        this.ObservableForProperty(x => x.SlideIndex)
            .Subscribe(_ => UpdateCanCompleteClassChanging());
        SettingsService.Settings.ObservableForProperty(x => x.IsSwapMode)
            .Subscribe(_ => UpdateCanCompleteClassChanging());
    }

    private void UpdateCanCompleteClassChanging()
    {
        if (SlideIndex == 0)
        {
            CanCompleteClassChanging = false;
            return;
        }

        if (SettingsService.Settings.IsSwapMode)
        {
            CanCompleteClassChanging = SwapModeTargetIndex != -1;
            return;
        }

        CanCompleteClassChanging = TargetSubjectIndex != Guid.Empty;
    }
}