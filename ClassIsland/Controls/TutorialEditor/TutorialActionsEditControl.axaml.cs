using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Core.Enums.Tutorial;
using ClassIsland.Core.Models.Tutorial;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Automation;

namespace ClassIsland.Controls.TutorialEditor;

public partial class TutorialActionsEditControl : UserControl
{
    public static readonly StyledProperty<ObservableCollection<TutorialAction>> TutorialActionsProperty = AvaloniaProperty.Register<TutorialActionsEditControl, ObservableCollection<TutorialAction>>(
        nameof(TutorialActions));

    public ObservableCollection<TutorialAction> TutorialActions
    {
        get => GetValue(TutorialActionsProperty);
        set => SetValue(TutorialActionsProperty, value);
    }

    public static readonly StyledProperty<TutorialAction?> SelectedItemProperty = AvaloniaProperty.Register<TutorialActionsEditControl, TutorialAction?>(
        nameof(SelectedItem));

    public TutorialAction? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }
    
    public TutorialActionsEditControl()
    {
        InitializeComponent();
    }

    private void ButtonAdd_OnClick(object? sender, RoutedEventArgs e)
    {
        TutorialActions.Add(new TutorialAction());
    }

    private void ButtonDuplicate_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedItem != null) 
            TutorialActions.Add(ConfigureFileHelper.CopyObject(SelectedItem));
    }

    private void ButtonDelete_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedItem != null) 
            TutorialActions.Remove(SelectedItem);
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { DataContext: TutorialAction action })
        {
            return;
        }

        if (action.Kind == TutorialActionKind.InvokeActionSet)
        {
            action.ActionSet ??= new ActionSet();
        }
    }
}