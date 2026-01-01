using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Labs.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Xaml.Interactions.DragAndDrop;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Assists;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Controls.Ruleset;
using ClassIsland.Core.Models.Components;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Shared;
using ClassIsland.ViewModels.EditMode;

namespace ClassIsland.Controls.EditMode;

public partial class EditModeView : UserControl
{
    public EditModeViewModel ViewModel { get; } = IAppHost.GetService<EditModeViewModel>();
    
    public EditModeView()
    {
        InitializeComponent();
        DataContext = this;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        ViewModel.MainViewModel.ContainerComponents.Clear();
    }

    private void OpenDrawer(string key, string? title = null, string? icon = null)
    {
        OpenDrawerCore(this.FindResource(key), icon == null
            ? title
            : new IconText
            {
                Glyph = icon,
                Text = title ?? ""
            });
    }

    private void OpenDrawerCore(object? content, object? title)
    {
        ViewModel.MainDrawerContent = content;
        ViewModel.MainDrawerTitle = title;
        ViewModel.MainDrawerState = VerticalDrawerOpenState.Opened;
    }
    
    
    private void CommandBindingOpenDrawer_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ViewModel.SecondaryDrawerContent = e.Parameter;
        ViewModel.SecondaryDrawerState = VerticalDrawerOpenState.Opened;
    }

    private void CommandBindingCloseDrawer_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        ViewModel.SecondaryDrawerState = VerticalDrawerOpenState.Closed;
    }

    public void OpenComponentsLibDrawer()
    {
        OpenDrawer("ComponentsDrawer", "组件库");
        // 重载组件库列表项目，修复切换视图后无法拖拽的问题。
        ViewModel.ComponentInfos = [];
        ViewModel.ComponentInfos = ComponentRegistryService.Registered;
    }
    
    public void OpenAppearanceSettingsDrawer()
    {
        OpenDrawer("AppearanceSettingsDrawer", "外观");
    }

    private void InstanceOnDragEnded()
    {
        if (!ViewModel.IsDrawerTempCollapsed)
        {
            return;
        }
        
        ViewModel.MainDrawerState = VerticalDrawerOpenState.Opened;
        ViewModel.IsDrawerTempCollapsed = false;
    }

    private void InstanceOnDragStarted()
    {
        if (ViewModel.MainDrawerState != VerticalDrawerOpenState.Opened)
        {
            return;
        }

        ViewModel.MainDrawerState = VerticalDrawerOpenState.Collapsed;
        ViewModel.IsDrawerTempCollapsed = true;
    }
    
    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        ManagedDragDropService.Instance.DragStarted += InstanceOnDragStarted;
        ManagedDragDropService.Instance.DragEnded += InstanceOnDragEnded;
    }

    private void Control_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        ManagedDragDropService.Instance.DragStarted -= InstanceOnDragStarted;
        ManagedDragDropService.Instance.DragEnded -= InstanceOnDragEnded;
    }

    private void SettingsExpanderNavigateAppearance_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!ViewModel.MainWindow.ViewModel.IsWindowMode)
        {
            ViewModel.MainWindow.ViewModel.IsEditMode = false;
        }
        ViewModel.UriNavigationService.NavigateWrapped(new Uri("classisland://app/settings/appearance"));
    }

    public void ShowComponentSettings()
    {
        OpenDrawerCore(this.FindResource("ComponentSettingsDrawer"), 
            this.FindResource("ComponentSettingsDrawerTitle"));
    }
    
    public void OpenChildComponents(ComponentSettings? settings, IReadOnlyList<ComponentSettings> stack, Point pos)
    {
        if (settings == null)
        {
            return;
        }
        var info = new EditModeContainerComponentInfo(settings, [..stack, settings])
        {
            X = pos.X,
            Y = pos.Y
        };
        var success = ViewModel.ContainerComponentCache.TryAdd(settings, info);
        if (!success)
        {
            return;
        }
        ViewModel.MainViewModel.ContainerComponents.Add(info);
        
    }
    
    private void ButtonOpenRuleset_OnClick(object? sender, RoutedEventArgs e)
    {
        if (this.FindResource("RulesetControl") is not RulesetControl control ||
            ViewModel.MainViewModel.SelectedComponentSettings == null) 
            return;
        control.Ruleset = ViewModel.MainViewModel.SelectedComponentSettings.HidingRules;
        SettingsPageBase.OpenDrawerCommand.Execute(control);
        ViewModel.SecondaryDrawerContent = control;
        ViewModel.SecondaryDrawerTitle = "编辑规则集";
        ViewModel.SecondaryDrawerState = VerticalDrawerOpenState.Opened;
    }


    public void CloseContainerComponent(EditModeContainerComponentInfo? info)
    {
        if (info == null)
        {
            return;
        }

        ViewModel.MainViewModel.ContainerComponents.Remove(info);
        ViewModel.ContainerComponentCache.Remove(info.Settings);
    }

    public void OpenMainWindowLineSettings(MainWindowLineSettings settings)
    {
        ViewModel.SelectedMainWindowLineSettings = settings;
        OpenDrawer("MainWindowLineSettingsDrawer", "主界面行设置");
    }
}