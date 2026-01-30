
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;
using DynamicData.Binding;

namespace ClassIsland.Controls;

public class ScheduleCalendarControl : Calendar
{
    public event EventHandler? ScheduleUpdated;

    public static readonly AttachedProperty<SyncDictionaryList<Guid, ClassPlan>> ClassPlanListProperty =
        AvaloniaProperty.RegisterAttached<ScheduleCalendarControl, Control, SyncDictionaryList<Guid, ClassPlan>>("ClassPlanList", inherits: true);

    private static void SetClassPlanList(Control obj, SyncDictionaryList<Guid, ClassPlan> value) => obj.SetValue(ClassPlanListProperty, value);
    public static SyncDictionaryList<Guid, ClassPlan> GetClassPlanList(Control obj) => obj.GetValue(ClassPlanListProperty);
    
    private List<IDisposable> _updateObservers = [];
    
    public IProfileService ProfileService { get; } = IAppHost.GetService<IProfileService>(); 

    public void UpdateSchedule()
    {
        ScheduleUpdated?.Invoke(this, EventArgs.Empty);
    }

    public ScheduleCalendarControl()
    {
        SetClassPlanList(this, new SyncDictionaryList<Guid, ClassPlan>(ProfileService.Profile.ClassPlans, Guid.NewGuid));
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }
    
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        UpdateObservers();
        UpdateSchedule();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        UnsubscribeAllObservers();
    }

    private void UpdateObservers()
    {
        UnsubscribeAllObservers();
        foreach (var (_, classPlan) in ProfileService.Profile.ClassPlans)
        {
            var observer = classPlan.TimeRule.WhenAnyPropertyChanged()
                .Subscribe(x => UpdateSchedule());
            _updateObservers.Add(observer);
        }

        var globalObserver = ProfileService.Profile.ClassPlans.ObserveCollectionChanges()
            .Subscribe(_ => UpdateSchedule());
        _updateObservers.Add(globalObserver);
        var profileObserver = ProfileService.Profile.WhenAnyPropertyChanged()
            .Subscribe(_ => UpdateSchedule());
        _updateObservers.Add(profileObserver);
    }

    private void UnsubscribeAllObservers()
    {
        foreach (var observer in _updateObservers)
        {
            observer.Dispose();
        }
        _updateObservers.Clear();
    }
    
}
