using System;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.Management;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Models.Profile;
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
    public ObservableCollection<SubjectSelectionItem> SubjectSelectionItems { get; } = [];

    public ClassChangingViewModel(IProfileService profileService, IManagementService managementService, SettingsService settingsService)
    {
        ProfileService = profileService;
        ManagementService = managementService;
        SettingsService = settingsService;

        Subjects = new SyncDictionaryList<Guid, Subject>(ProfileService.Profile.Subjects, Guid.NewGuid);
        RefreshSubjectSelectionItems();
        ProfileService.Profile.Subjects.CollectionChanged += SubjectsOnCollectionChanged;
        ProfileService.Profile.SubjectGroups.CollectionChanged += (_, _) => RefreshSubjectSelectionItems();
        foreach (var subject in ProfileService.Profile.Subjects.Values)
        {
            subject.PropertyChanged += SubjectOnPropertyChanged;
        }
        this.ObservableForProperty(x => x.TargetSubjectIndex)
            .Subscribe(_ => UpdateCanCompleteClassChanging());
        this.ObservableForProperty(x => x.SwapModeTargetIndex)
            .Subscribe(_ => UpdateCanCompleteClassChanging());
        this.ObservableForProperty(x => x.SlideIndex)
            .Subscribe(_ => UpdateCanCompleteClassChanging());
        SettingsService.Settings.ObservableForProperty(x => x.IsSwapMode)
            .Subscribe(_ => UpdateCanCompleteClassChanging());
    }

    private void SubjectsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var item in e.OldItems?.OfType<KeyValuePair<Guid, Subject>>() ?? [])
        {
            item.Value.PropertyChanged -= SubjectOnPropertyChanged;
        }
        foreach (var item in e.NewItems?.OfType<KeyValuePair<Guid, Subject>>() ?? [])
        {
            item.Value.PropertyChanged += SubjectOnPropertyChanged;
        }
        RefreshSubjectSelectionItems();
    }

    private void SubjectOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Subject.GroupId))
        {
            RefreshSubjectSelectionItems();
        }
    }

    private void RefreshSubjectSelectionItems()
    {
        var items = new List<SubjectSelectionItem>();
        var groups = ProfileService.Profile.SubjectGroups;
        var subjects = ProfileService.Profile.Subjects;
        foreach (var group in groups)
        {
            items.Add(new SubjectSelectionItem(Guid.Empty, null, group.Value.Name));
            items.AddRange(subjects.Where(x => x.Value.GroupId == group.Key)
                .Select(x => new SubjectSelectionItem(x.Key, x.Value)));
        }
        items.Add(new SubjectSelectionItem(Guid.Empty, null, "未分组"));
        items.AddRange(subjects
            .Where(x => x.Value.GroupId == Guid.Empty || !groups.ContainsKey(x.Value.GroupId))
            .Select(x => new SubjectSelectionItem(x.Key, x.Value)));

        SubjectSelectionItems.Clear();
        foreach (var item in items)
        {
            SubjectSelectionItems.Add(item);
        }
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
