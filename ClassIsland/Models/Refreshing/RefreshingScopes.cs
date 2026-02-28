using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Refreshing;

public partial class RefreshingScopes : ObservableObject
{
    [ObservableProperty] private bool _profile;
    [ObservableProperty] private bool _profileClassPlans = true;
    [ObservableProperty] private bool _profileTimeLayouts;
    [ObservableProperty] private bool _settings;
    [ObservableProperty] private bool _components;
    [ObservableProperty] private bool _automations;

    [ObservableProperty] private ObservableCollection<Guid> _reservedClassPlans = [];
    [ObservableProperty] private ObservableCollection<Guid> _reservedTimeLayouts = [];
}