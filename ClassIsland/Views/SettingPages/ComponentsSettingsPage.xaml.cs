using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
using GongSolutions.Wpf.DragDrop;
using MaterialDesignThemes.Wpf;
using Path = System.IO.Path;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// ComponentsSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("components", "组件", PackIconKind.WidgetsOutline, PackIconKind.Widgets, SettingsPageCategory.Internal)]
public partial class ComponentsSettingsPage : SettingsPageBase
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
    }

    private void ButtonRemoveSelectedComponent_OnClick(object sender, RoutedEventArgs e)
    {
        var remove = ViewModel.SelectedComponentSettings;
        if (remove == null)
            return;
        if (ViewModel.SelectedContainerComponent == ViewModel.SelectedComponentSettings)
        {
            ViewModel.IsComponentChildrenViewOpen = false;
            ViewModel.SelectedComponentContainerChildren = [];
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
        ViewModel.IsComponentChildrenViewOpen = false;
    }

    private void ButtonNavigateUp_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.ChildrenComponentSettingsNavigationStack.TryPop(out var settings))
        {
            return;
        }
        SetCurrentSelectedComponentContainer(settings, true);
    }
}