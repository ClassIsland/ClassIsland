using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Models;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Controls.Ruleset;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Components;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.ViewModels.SettingsPages;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using ReactiveUI;

namespace ClassIsland.Views.SettingPages;

[SettingsPageInfo("components", "组件", "\ue06f", "\ue06f", SettingsPageCategory.Internal)]
public partial class ComponentsSettingsPage : SettingsPageBase
{
    public ComponentsSettingsViewModel ViewModel { get; } = IAppHost.GetService<ComponentsSettingsViewModel>();
    
    public ComponentsSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
        ViewModel.SettingsService.Settings
            .ObservableForProperty(x => x.CurrentComponentConfig)
            .Subscribe(_ => ClearSelectedComponents());
    }
    
    private void ButtonRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        ClearSelectedComponents();
        ViewModel.ComponentsService.RefreshConfigs();
    }

    private void ClearSelectedComponents()
    {
        ViewModel.SelectedComponentSettingsMain = null;
        ViewModel.SelectedComponentSettings = null;
        CloseComponentChildrenView();
        UpdateSettingsVisibility();
    }

    private async void ButtonCreateConfig_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateProfileName = "";

        var textBox = new TextBox()
        {
            Text = ""
        };
        var dialogResult = await new ContentDialog()
        {
            Title = "创建组件配置",
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "创建",
            SecondaryButtonText = "取消",
            Content = new Field()
            {
                Content = textBox,
                Label = "组件名",
                Suffix = ".json"
            }
        }.ShowAsync();

        ViewModel.CreateProfileName = textBox.Text;
        var path = Path.Combine(ClassIsland.Services.ComponentsService.ComponentSettingsPath,
            ViewModel.CreateProfileName + ".json");
        if (dialogResult != ContentDialogResult.Primary || File.Exists(path))
        {
            return;
        }
        ConfigureFileHelper.SaveConfig(path, ClassIsland.Services.ComponentsService.DefaultComponentProfile);
        ViewModel.ComponentsService.RefreshConfigs();
        ViewModel.SettingsService.Settings.CurrentComponentConfig = ViewModel.CreateProfileName;
        ViewModel.SelectedComponentSettings = null;
        UpdateSettingsVisibility();
    }
    
    private void SelectorComponents_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count <= 0 || e.AddedItems[0] is not ComponentSettings settings)
        {
            UpdateSettingsVisibility();
            return;
        }
        foreach (var listBox in ViewModel.MainWindowLineListBoxCacheReversed.Keys.Where(x => !Equals(x, sender)))
        {
            listBox.SelectedItem = null;
        }
        ListBoxComponentsChildren.SelectedItem = null;
        ViewModel.SelectedComponentSettingsMain = settings;
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
        ViewModel.IsSelectedComponentOnRoot =
            ViewModel.SelectedComponentSettings == ViewModel.SelectedComponentSettingsMain;
        if (ViewModel.SelectedComponentSettings.AssociatedComponentInfo.SettingsType == null)
        {
            ViewModel.IsComponentSettingsVisible = false;
            ViewModel.SettingsTabControlIndex = ViewModel.SettingsTabControlIndex == 1 ? 0 : ViewModel.SettingsTabControlIndex;
            return;
        }
        ViewModel.IsComponentSettingsVisible = true;
        ViewModel.SettingsTabControlIndex = ViewModel.SettingsTabControlIndex == 0 ? 1 : ViewModel.SettingsTabControlIndex;
    }

    private void ButtonOpenConfigFolder_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath(ClassIsland.Services.ComponentsService.ComponentSettingsPath),
            UseShellExecute = true
        });
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
            foreach (var line in ViewModel.ComponentsService.CurrentComponents.Lines)
            {
                if (line.Children.Remove(remove))
                {
                    break;
                }
            }
        } else if (ViewModel.SelectedComponentSettingsChild != null)
        {
            ViewModel.SelectedComponentContainerChildren.Remove(remove);
        }
    }
    
    private void ButtonChildrenViewClose_OnClick(object sender, RoutedEventArgs e)
    {
        CloseComponentChildrenView();
    }
    
    private void CloseComponentChildrenView()
    {
        ViewModel.IsComponentChildrenViewOpen = false;
        ViewModel.ChildrenComponentSettingsNavigationStack.Clear();
        ViewModel.CanChildrenNavigateBack = false;
        ViewModel.SelectedComponentContainerChildren = [];
        ViewModel.SelectedRootComponent = null;
    }
    
    private void ButtonOpenRuleset_OnClick(object sender, RoutedEventArgs e)
    {
        if (this.FindResource("RulesetControl") is not RulesetControl control ||
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

    private void ListBoxComponents_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 这里应该把指针按下的事件吞掉，这样在拖动这里面的在组件时就不会把事件往上传送到上级，让上级 ListBoxItem 也跟着被拖动。
        e.Handled = true;
    }

    private void ButtonCreateMainWindowLine_OnClick(object? sender, RoutedEventArgs e)
    {
        var index = ViewModel.SelectedMainWindowLineSettings == null
            ? 0
            : Math.Max(0,
                ViewModel.ComponentsService.CurrentComponents.Lines.IndexOf(ViewModel.SelectedMainWindowLineSettings) + 1);
        var lineSettings = new MainWindowLineSettings();
        ViewModel.ComponentsService.CurrentComponents.Lines.Insert(index, lineSettings);
        ViewModel.SelectedMainWindowLineSettings = lineSettings;
    }

    private void ButtonRemoveSelectedMainWindowLine_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.ComponentsService.CurrentComponents.Lines.Count <= 1)
        {
            this.ShowWarningToast("至少需要保留 1 个主界面行。");
            return;
        }

        if (ViewModel.SelectedMainWindowLineSettings != null)
            ViewModel.ComponentsService.CurrentComponents.Lines.Remove(ViewModel.SelectedMainWindowLineSettings);
    }

    private void ToggleButtonIsNotificationEnabled_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (!ViewModel.ComponentsService.CurrentComponents.Lines.Any(x => x.IsNotificationEnabled))
        {
            this.ShowWarningToast("您已经禁用了所有主界面行的提醒显示功能。如果没有插件注册其它提醒消费者，提醒将不会显示，也不会播放提醒音效、特效和语音。");
        }
    }

    private void ToggleButtonIsMainLine_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton button)
        {
            return;
        }

        foreach (var line in ViewModel.ComponentsService.CurrentComponents.Lines.Where(x => !Equals(button.DataContext, x)))
        {
            line.IsMainLine = false;
        }

        if (button.IsChecked == false)
        {
            var firstLine = ViewModel.ComponentsService.CurrentComponents.Lines.FirstOrDefault();
            if (firstLine != null) 
                firstLine.IsMainLine = true;
            this.ShowToast("已将第一行设置为主要行。");
        }
    }

    private void ListBoxMainWindowLineSettings_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not ListBox { Tag: MainWindowLineSettings settings } listBox)
        {
            return;
        }

        ViewModel.MainWindowLineListBoxCache[settings] = listBox;
        ViewModel.MainWindowLineListBoxCacheReversed[listBox] = settings;
    }

    private void ListBoxMainWindowLineSettings_OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not ListBox listBox)
        {
            return;
        }

        var settings = ViewModel.MainWindowLineListBoxCacheReversed.GetValueOrDefault(listBox);
        if (settings != null)
        {
            ViewModel.MainWindowLineListBoxCache.Remove(settings);
        }
        ViewModel.MainWindowLineListBoxCacheReversed.Remove(listBox);
    }

    private void ButtonNavigateUp_OnClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.ChildrenComponentSettingsNavigationStack.TryPop(out var settings))
        {
            return;
        }
        SetCurrentSelectedComponentContainer(settings, true);
    }

    private void SelectorComponentsChildren_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {

        if (e.AddedItems.Count <= 0 || e.AddedItems[0] is not ComponentSettings settings)
        {
            UpdateSettingsVisibility();
            return;
        }
        foreach (var listBox in ViewModel.MainWindowLineListBoxCacheReversed.Keys.Where(x => !Equals(x, sender)))
        {
            listBox.SelectedItem = null;
        }
        
        ViewModel.SelectedComponentSettingsChild = settings;
        ViewModel.SelectedComponentSettings = ViewModel.SelectedComponentSettingsChild;
        ViewModel.SelectedComponentSettingsMain = null;
        UpdateSettingsVisibility();
    }

    [RelayCommand]
    private void DuplicateComponent(ComponentSettings settings)
    {
        var list = GetSelectedComponentSource(settings);
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
    
    [RelayCommand]
    private void CreateContainerComponent(ComponentInfo container)
    {
        if (ViewModel.SelectedComponentSettings == null)
        {
            return;
        }

        var selected = ViewModel.SelectedComponentSettings;
        var list = GetSelectedComponentSource(selected);
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

    private ObservableCollection<ComponentSettings> GetSelectedComponentSource(ComponentSettings selected)
    {
        return ViewModel.ComponentsService.CurrentComponents.Lines
            .FirstOrDefault(x => x.Children.Contains(selected))?
            .Children ?? ViewModel.SelectedComponentContainerChildren;
    }

    [RelayCommand]
    private void MoveComponentToPreviousLine(ComponentSettings settings)
    {
        var list = GetSelectedComponentSource(settings);
        if (list == ViewModel.SelectedComponentContainerChildren)
        {
            return;
        }

        var index = -1;
        for (var i = 0; i < ViewModel.ComponentsService.CurrentComponents.Lines.Count; i++)
        {
            var line = ViewModel.ComponentsService.CurrentComponents.Lines[i];
            if (line.Children != list) 
                continue;
            index = i;
        }

        if (index == -1)
        {
            return;
        }

        list.Remove(settings);
        if (index - 1 >= 0)
        {
            ViewModel.ComponentsService.CurrentComponents.Lines[index - 1].Children.Add(settings);
            return;
        }

        var newLine = new MainWindowLineSettings()
        {
            Children =
            {
                settings
            }
        };
        ViewModel.ComponentsService.CurrentComponents.Lines.Insert(0, newLine);
        this.ShowToast("已向上创建新主界面行。");
    }
    
    [RelayCommand]
    private void MoveComponentToNextLine(ComponentSettings settings)
    {
        var list = GetSelectedComponentSource(settings);
        if (list == ViewModel.SelectedComponentContainerChildren)
        {
            return;
        }

        var index = -1;
        for (var i = 0; i < ViewModel.ComponentsService.CurrentComponents.Lines.Count; i++)
        {
            var line = ViewModel.ComponentsService.CurrentComponents.Lines[i];
            if (line.Children != list) 
                continue;
            index = i;
        }

        if (index == -1)
        {
            return;
        }

        list.Remove(settings);
        if (index + 1 < ViewModel.ComponentsService.CurrentComponents.Lines.Count)
        {
            ViewModel.ComponentsService.CurrentComponents.Lines[index + 1].Children.Add(settings);
            return;
        }

        var newLine = new MainWindowLineSettings()
        {
            Children =
            {
                settings
            }
        };
        ViewModel.ComponentsService.CurrentComponents.Lines.Add(newLine);
        this.ShowToast("已向下创建新主界面行。");
    }

    [RelayCommand]
    private void MoveToCurrentContainerComponent(ComponentSettings settings)
    {
        var list = GetSelectedComponentSource(settings);
        if (list == ViewModel.SelectedComponentContainerChildren)
        {
            return;
        }
        
        if (settings == ViewModel.SelectedRootComponent)
        {
            this.ShowWarningToast("不能将容器组件移动到自身（或其子级）的子组件中。");
            return;
        }
        
        list.Remove(settings);
        ViewModel.SelectedComponentContainerChildren.Add(settings);
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            ViewModel.SelectedComponentSettingsChild = settings;
        });
    }

    [RelayCommand]
    private void MoveComponentsToMainLines(ComponentSettings settings)
    {
        if (!ViewModel.SelectedComponentContainerChildren.Remove(settings))
        {
            return;
        }

        var selectedList = ViewModel.SelectedMainWindowLineSettings?.Children ??
                           ViewModel.ComponentsService.CurrentComponents.Lines.FirstOrDefault()?.Children;

        selectedList?.Add(settings);
    }

    [RelayCommand]
    private void AddSelectedComponentToMainLines(ComponentInfo info)
    {
        var selectedList = ViewModel.SelectedMainWindowLineSettings?.Children ??
                           ViewModel.ComponentsService.CurrentComponents.Lines.FirstOrDefault()?.Children;
        if (selectedList == null)
        {
            return;
        }
        
        var componentSettings = new ComponentSettings()
        {
            Id = info.Guid.ToString()
        };
        selectedList.Add(componentSettings);
        ComponentsService.LoadComponentSettings(componentSettings,
            componentSettings.AssociatedComponentInfo.ComponentType!.BaseType!); 
    }
}