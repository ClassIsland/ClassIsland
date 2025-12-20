using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Xaml.Interactions.DragAndDrop;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
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

    private void OpenDrawer(string key, string? title = null, string? icon = null)
    {
        ViewModel.MainDrawerContent = this.FindResource(key);
        ViewModel.MainDrawerTitle = icon == null
            ? title
            : new IconText
            {
                Glyph = icon,
                Text = title ?? ""
            };
        ViewModel.MainDrawerState = VerticalDrawerOpenState.Opened;
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
}