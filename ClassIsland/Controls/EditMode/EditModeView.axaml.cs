using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Xaml.Interactions.DragAndDrop;
using ClassIsland.Core.Controls;
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
}