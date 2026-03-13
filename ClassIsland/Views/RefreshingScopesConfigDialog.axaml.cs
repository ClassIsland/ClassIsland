using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Core.Controls;
using ClassIsland.Models.Refreshing;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Views;

public partial class RefreshingScopesConfigDialog : MyWindow
{
    public IProfileService ProfileService { get; } = IAppHost.GetService<IProfileService>();
    
    public required RefreshingScopes Scopes { get; init; }
    
    public SyncDictionaryList<Guid, ClassPlan> ClassPlans { get; }
    public SyncDictionaryList<Guid, TimeLayout> TimeLayouts { get; }

    private bool _isSyncingClassPlansSelection;
    private bool _isSyncingTimeLayoutsSelection;
    
    public RefreshingScopesConfigDialog()
    {
        ClassPlans = new SyncDictionaryList<Guid, ClassPlan>(ProfileService.Profile.ClassPlans, Guid.NewGuid);
        TimeLayouts = new SyncDictionaryList<Guid, TimeLayout>(ProfileService.Profile.TimeLayouts, Guid.NewGuid);
        
        DataContext = this;
        InitializeComponent();
    }

    private void TopLevel_OnOpened(object? sender, EventArgs e)
    {
        HookScopesReservedClassPlansCollection();
        HookScopesReservedTimeLayoutsCollection();
        Scopes.PropertyChanged += ScopesOnPropertyChanged;
        SyncReservedClassPlansToSelection();
        SyncReservedTimeLayoutsToSelection();
    }

    private void TopLevel_OnClosed(object? sender, EventArgs e)
    {
        UnhookScopesReservedClassPlansCollection();
        UnhookScopesReservedTimeLayoutsCollection();
        Scopes.PropertyChanged -= ScopesOnPropertyChanged;
    }

    private void ButtonDone_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ListBoxClassPlans_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SyncSelectionToReservedClassPlans();
    }

    private void ListBoxTimeLayouts_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SyncSelectionToReservedTimeLayouts();
    }

    private void ScopesOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RefreshingScopes.ReservedClassPlans))
        {
            UnhookScopesReservedClassPlansCollection();
            HookScopesReservedClassPlansCollection();
            SyncReservedClassPlansToSelection();
        }
        
        if (e.PropertyName == nameof(RefreshingScopes.ReservedTimeLayouts))
        {
            UnhookScopesReservedTimeLayoutsCollection();
            HookScopesReservedTimeLayoutsCollection();
            SyncReservedTimeLayoutsToSelection();
        }
    }

    private void ReservedClassPlansOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncReservedClassPlansToSelection();
    }

    private void HookScopesReservedClassPlansCollection()
    {
        Scopes.ReservedClassPlans.CollectionChanged += ReservedClassPlansOnCollectionChanged;
    }

    private void UnhookScopesReservedClassPlansCollection()
    {
        Scopes.ReservedClassPlans.CollectionChanged -= ReservedClassPlansOnCollectionChanged;
    }

    private void ReservedTimeLayoutsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncReservedTimeLayoutsToSelection();
    }

    private void HookScopesReservedTimeLayoutsCollection()
    {
        Scopes.ReservedTimeLayouts.CollectionChanged += ReservedTimeLayoutsOnCollectionChanged;
    }

    private void UnhookScopesReservedTimeLayoutsCollection()
    {
        Scopes.ReservedTimeLayouts.CollectionChanged -= ReservedTimeLayoutsOnCollectionChanged;
    }

    private void SyncSelectionToReservedClassPlans()
    {
        if (_isSyncingClassPlansSelection)
        {
            return;
        }

        _isSyncingClassPlansSelection = true;
        try
        {
            var selectedKeys = ListBoxClassPlans.SelectedItems?
                .OfType<KeyValuePair<Guid, ClassPlan>>()
                .Select(x => x.Key)
                .ToHashSet() ?? [];

            for (var i = Scopes.ReservedClassPlans.Count - 1; i >= 0; i--)
            {
                if (!selectedKeys.Contains(Scopes.ReservedClassPlans[i]))
                {
                    Scopes.ReservedClassPlans.RemoveAt(i);
                }
            }

            foreach (var key in selectedKeys)
            {
                if (!Scopes.ReservedClassPlans.Contains(key))
                {
                    Scopes.ReservedClassPlans.Add(key);
                }
            }
        }
        finally
        {
            _isSyncingClassPlansSelection = false;
        }
    }

    private void SyncReservedClassPlansToSelection()
    {
        if (_isSyncingClassPlansSelection)
        {
            return;
        }

        _isSyncingClassPlansSelection = true;
        try
        {
            if (ListBoxClassPlans.SelectedItems == null)
            {
                return;
            }

            ListBoxClassPlans.SelectedItems.Clear();
            foreach (var classPlan in ClassPlans.List)
            {
                if (Scopes.ReservedClassPlans.Contains(classPlan.Key))
                {
                    ListBoxClassPlans.SelectedItems.Add(classPlan);
                }
            }
        }
        finally
        {
            _isSyncingClassPlansSelection = false;
        }
    }

    private void SyncSelectionToReservedTimeLayouts()
    {
        if (_isSyncingTimeLayoutsSelection)
        {
            return;
        }

        _isSyncingTimeLayoutsSelection = true;
        try
        {
            var selectedKeys = ListBoxTimeLayouts.SelectedItems?
                .OfType<KeyValuePair<Guid, TimeLayout>>()
                .Select(x => x.Key)
                .ToHashSet() ?? [];

            for (var i = Scopes.ReservedTimeLayouts.Count - 1; i >= 0; i--)
            {
                if (!selectedKeys.Contains(Scopes.ReservedTimeLayouts[i]))
                {
                    Scopes.ReservedTimeLayouts.RemoveAt(i);
                }
            }

            foreach (var key in selectedKeys)
            {
                if (!Scopes.ReservedTimeLayouts.Contains(key))
                {
                    Scopes.ReservedTimeLayouts.Add(key);
                }
            }
        }
        finally
        {
            _isSyncingTimeLayoutsSelection = false;
        }
    }

    private void SyncReservedTimeLayoutsToSelection()
    {
        if (_isSyncingTimeLayoutsSelection)
        {
            return;
        }

        _isSyncingTimeLayoutsSelection = true;
        try
        {
            if (ListBoxTimeLayouts.SelectedItems == null)
            {
                return;
            }

            ListBoxTimeLayouts.SelectedItems.Clear();
            foreach (var timeLayout in TimeLayouts.List)
            {
                if (Scopes.ReservedTimeLayouts.Contains(timeLayout.Key))
                {
                    ListBoxTimeLayouts.SelectedItems.Add(timeLayout);
                }
            }
        }
        finally
        {
            _isSyncingTimeLayoutsSelection = false;
        }
    }
}
