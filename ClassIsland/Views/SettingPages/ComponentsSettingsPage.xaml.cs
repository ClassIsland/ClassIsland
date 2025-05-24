using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Models;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls.Ruleset;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Models.Components;
using ClassIsland.Services;
using ClassIsland.Shared.Helpers;
using ClassIsland.ViewModels.SettingsPages;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using MaterialDesignThemes.Wpf;
using Path = System.IO.Path;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// ComponentsSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("components", "组件", PackIconKind.WidgetsOutline, PackIconKind.Widgets, SettingsPageCategory.Internal)]
public partial class ComponentsSettingsPage : SettingsPageBase, IDropTarget
{
    public IComponentsService ComponentsService { get; }

    public ComponentsSettingsViewModel ViewModel { get; } = new();

    public SettingsService SettingsService { get; }

    public ComponentsSettingsPage(IComponentsService componentsService, SettingsService settingsService)
    {
        SettingsService = settingsService;
        ComponentsService = componentsService;
        InitializeComponent();
        
        DataContext = this;
        var mainHandler = FindResource("MainComponentsSettingsPageDropHandler") as ComponentsSettingsPageDropHandler;
        var childHandler = FindResource("ChildComponentsSettingsPageDropHandler") as ComponentsSettingsPageDropHandler;
        if (mainHandler is not null)
        {
            mainHandler.Components = ComponentsService.CurrentComponents;
        }
        if (FindResource("DataProxy") is BindingProxy proxy)
        {
            proxy.Data = this;
        }
        if (FindResource("DataProxyDuplicate") is BindingProxy proxyDuplicate)
        {
            proxyDuplicate.Data = DuplicateComponentCommand;
        }
    }

    private void OnSettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(SettingsService.Settings.CurrentComponentConfig))
        {
            CloseComponentChildrenView();
        }
    }

    private void CloseComponentChildrenView()
    {
        ViewModel.IsComponentChildrenViewOpen = false;
        ViewModel.ChildrenComponentSettingsNavigationStack.Clear();
        ViewModel.CanChildrenNavigateBack = false;
        ViewModel.SelectedComponentContainerChildren = [];
        ViewModel.SelectedRootComponent = null;
    }

    private void ButtonRemoveSelectedComponent_OnClick(object sender, RoutedEventArgs e)
    {
        var remove = ViewModel.SelectedComponentSettings;
        if (remove == null)
            return;
        if (ViewModel.SelectedComponentSettings == ViewModel.SelectedRootComponent)
        {
            CloseComponentChildrenView();
        }
        ViewModel.SelectedComponentSettings = null;
        if (ViewModel.SelectedComponentSettingsMain != null)
        {
            ComponentsService.CurrentComponents.Remove(remove);
        } else if (ViewModel.SelectedComponentSettingsChild != null)
        {
            ViewModel.SelectedComponentContainerChildren.Remove(remove);
        }
    }

    private void ButtonRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        ComponentsService.RefreshConfigs();
    }

    private async void ButtonCreateConfig_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateProfileName = "";
        if (FindResource("CreateProfileDialog") is not FrameworkElement content)
            return;
        content.DataContext = this;
        var r = await DialogHost.Show(content, DialogHostIdentifier);
        Debug.WriteLine(r);

        var path = Path.Combine(ClassIsland.Services.ComponentsService.ComponentSettingsPath,
            ViewModel.CreateProfileName + ".json");
        if (r == null || File.Exists(path))
        {
            return;
        }
        ConfigureFileHelper.SaveConfig(path, ClassIsland.Services.ComponentsService.DefaultComponents);
        ComponentsService.RefreshConfigs();
        SettingsService.Settings.CurrentComponentConfig = ViewModel.CreateProfileName;
    }

    private void ButtonOpenConfigFolder_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath(ClassIsland.Services.ComponentsService.ComponentSettingsPath),
            UseShellExecute = true
        });
    }

    private void SelectorComponents_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.SelectedComponentSettingsMain != null)
        {
            ViewModel.SelectedComponentSettings = ViewModel.SelectedComponentSettingsMain;
            ViewModel.SelectedComponentSettingsChild = null;
        }
        UpdateSettingsVisibility();
    }

    private void UpdateSettingsVisibility()
    {
        if (ViewModel.SelectedComponentSettings == null)
        {
            ViewModel.IsComponentAdvancedSettingsVisible = false;
            ViewModel.IsComponentSettingsVisible = false;
            ViewModel.SettingsTabControlIndex = 0;
            return;
        }

        ViewModel.IsComponentAdvancedSettingsVisible = true;
        if (ViewModel.SelectedComponentSettings.AssociatedComponentInfo.SettingsType == null)
        {
            ViewModel.IsComponentSettingsVisible = false;
            ViewModel.SettingsTabControlIndex = ViewModel.SettingsTabControlIndex == 1 ? 0 : ViewModel.SettingsTabControlIndex;
            return;
        }
        ViewModel.IsComponentSettingsVisible = true;
        ViewModel.SettingsTabControlIndex = ViewModel.SettingsTabControlIndex == 0 ? 1 : ViewModel.SettingsTabControlIndex;
    }

    private void ButtonOpenRuleset_OnClick(object sender, RoutedEventArgs e)
    {
        if (FindResource("RulesetControl") is not RulesetControl control ||
            ViewModel.SelectedComponentSettings == null) 
            return;
        control.Ruleset = ViewModel.SelectedComponentSettings.HidingRules;
        OpenDrawer("RulesetControl");
    }

    private void ButtonShowChildrenComponents_OnClick(object sender, RoutedEventArgs e)
    {
        SetCurrentSelectedComponentContainer(ViewModel.SelectedComponentSettings);
    }

    private void SetCurrentSelectedComponentContainer(ComponentSettings? componentSettings, bool isBack=false)
    {
        if (componentSettings?.AssociatedComponentInfo?.IsComponentContainer != true)
        {
            return;
        }

        if (componentSettings.Settings is not IComponentContainerSettings settings)
        {
            return;
        }
        if (componentSettings == ViewModel.SelectedComponentSettingsMain)
        {
            ViewModel.ChildrenComponentSettingsNavigationStack.Clear();
            ViewModel.SelectedRootComponent = componentSettings;
        } else if (ViewModel.SelectedContainerComponent != null && !isBack)
        {
            ViewModel.ChildrenComponentSettingsNavigationStack.Push(ViewModel.SelectedContainerComponent);
        }
        ViewModel.SelectedComponentContainerChildren = settings.Children;
        ViewModel.SelectedContainerComponent = componentSettings;
        ViewModel.IsComponentChildrenViewOpen = true;
        ViewModel.CanChildrenNavigateBack = ViewModel.ChildrenComponentSettingsNavigationStack.Count >= 1;
    }

    private void SelectorComponentsChildren_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.SelectedComponentSettingsChild != null)
        {
            ViewModel.SelectedComponentSettings = ViewModel.SelectedComponentSettingsChild;
            ViewModel.SelectedComponentSettingsMain = null;
        }
        UpdateSettingsVisibility();
    }

    private void ButtonChildrenViewClose_OnClick(object sender, RoutedEventArgs e)
    {
        CloseComponentChildrenView();
    }

    private void ButtonNavigateUp_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.ChildrenComponentSettingsNavigationStack.TryPop(out var settings))
        {
            return;
        }
        SetCurrentSelectedComponentContainer(settings, true);
    }

    public new void DragOver(IDropInfo dropInfo)
    {
        // TODO: 如果拖入的组件是当前组件的父组件，要拒绝拖入到子容器中。
        if (dropInfo.Data is not ComponentInfo && dropInfo.Data is not ComponentSettings)
            return;
        if (dropInfo.Data is ComponentSettings settings && settings == ViewModel.SelectedRootComponent 
            && Equals(dropInfo.TargetCollection, ViewModel.SelectedComponentContainerChildren))
            return;
        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        dropInfo.Effects = dropInfo.Data switch
        {
            ComponentInfo => DragDropEffects.Copy,
            ComponentSettings => DragDropEffects.Move,
            _ => DragDropEffects.None
        };
    }

    public new void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.TargetCollection is not ObservableCollection<ComponentSettings> components)
        {
            return;
        }
        switch (dropInfo.Data)
        {
            case ComponentInfo info:
                var componentSettings = new ComponentSettings()
                {
                    Id = info.Guid.ToString()
                };
                components.Insert(dropInfo.InsertIndex, componentSettings);
                ClassIsland.Services.ComponentsService.LoadComponentSettings(componentSettings,
                    componentSettings.AssociatedComponentInfo.ComponentType!.BaseType!);
                break;
            case ComponentSettings settings:
                var oldIndex = components.IndexOf(settings);
                var newIndex = oldIndex < dropInfo.UnfilteredInsertIndex ? dropInfo.UnfilteredInsertIndex - 1 : dropInfo.UnfilteredInsertIndex;
                var finalIndex = newIndex >= components.Count ? components.Count - 1 : newIndex;
                if (!components.Contains(settings))
                {
                    var source = dropInfo.DragInfo.SourceCollection as ObservableCollection<ComponentSettings>;
                    source?.Remove(settings);
                    components.Insert(dropInfo.UnfilteredInsertIndex, settings);
                    break;
                }
                if (oldIndex != finalIndex)
                {
                    components.Move(oldIndex, finalIndex);
                }
                break;
        }
    }

    private void ButtonMoveToPrevLine_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedComponentSettings != null) 
            ViewModel.SelectedComponentSettings.RelativeLineNumber--;
    }

    private void ButtonMoveToNextLine_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedComponentSettings != null)
            ViewModel.SelectedComponentSettings.RelativeLineNumber++;
    }

    private void ComponentsSettingsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.PropertyChanged += OnSettingsOnPropertyChanged;
    }

    private void ComponentsSettingsPage_OnUnloaded(object sender, RoutedEventArgs e)
    {
        SettingsService.Settings.PropertyChanged -= OnSettingsOnPropertyChanged;
    }

    private void ContainerComponentsSource_OnFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is not ComponentInfo info)
        {
            return;
        }

        e.Accepted = info.IsComponentContainer;
    }

    [RelayCommand]
    private void OpenContextMenu(FrameworkElement element)
    {
        if (element.ContextMenu == null)
        {
            return;
        }

        //element.ContextMenu.DataContext = this;
        element.ContextMenu.IsOpen = true;
    }

    [RelayCommand]
    private void CreateContainerComponent(ComponentInfo container)
    {
        if (ViewModel.SelectedComponentSettings == null)
        {
            return;
        }

        var selected = ViewModel.SelectedComponentSettings;
        var list = ComponentsService.CurrentComponents.Contains(selected)
            ? ComponentsService.CurrentComponents
            : ViewModel.SelectedComponentContainerChildren;
        var index = list.IndexOf(ViewModel.SelectedComponentSettings);

        if (index == -1)
        {
            return;
        }

        index = Math.Min(list.Count - 1, index);
        var newComp = new ComponentSettings()
        {
            Id = container.Guid.ToString(),
        };
        if (container.ComponentType?.BaseType != null)
        {
            newComp.Settings =
                Services.ComponentsService.LoadComponentSettings(newComp, container.ComponentType.BaseType);
        }
        list.Insert(index, newComp);
        if (selected == ViewModel.SelectedComponentSettingsMain)
        {
            ViewModel.SelectedComponentSettingsMain = newComp;
        }
        else
        {
            ViewModel.SelectedComponentSettingsChild = newComp;
        }
        SetCurrentSelectedComponentContainer(newComp);
        list.Remove(selected);
        newComp.Children?.Add(selected);
        ViewModel.SelectedComponentSettings = newComp;
    }

    [RelayCommand]
    private void DuplicateComponent(ComponentSettings settings)
    {
        var list = ComponentsService.CurrentComponents.Contains(settings)
            ? ComponentsService.CurrentComponents
            : ViewModel.SelectedComponentContainerChildren;
        var index = list.IndexOf(settings);
        if (index == -1)
        {
            return;
        }
        index = Math.Min(list.Count - 1, index);

        var newSettings = ConfigureFileHelper.CopyObject(settings);
        list.Insert(index, newSettings);
        if (settings == ViewModel.SelectedComponentSettingsMain)
        {
            ViewModel.SelectedComponentSettingsMain = newSettings;
        }
        else
        {
            ViewModel.SelectedComponentSettingsChild = newSettings;
        }
        ViewModel.SelectedComponentSettings = newSettings;
    }

    private void MenuItemDuplicateComponent_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedComponentSettings != null)
        {
            DuplicateComponent(ViewModel.SelectedComponentSettings);
        }
    }
}