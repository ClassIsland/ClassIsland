using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactions.DragAndDrop;
using ClassIsland.Core.Abstractions.Behaviors;
using ClassIsland.Core.Models.Tutorial;

namespace ClassIsland.Views;

public sealed class TutorialEditorDragDropHandler : BaseDataGridDropHandler<TutorialSentence>
{
    protected override TutorialSentence MakeCopy(ObservableCollection<TutorialSentence> parentCollection, TutorialSentence dragItem) =>
        new() { Title = dragItem.Title };

    protected override bool Validate(DataGrid dg, DragEventArgs e, object? sourceContext, object? targetContext, bool execute)
    {
        if (sourceContext is not TutorialSentence sourceItem
            || targetContext is not TutorialParagraph vm
            || dg.GetVisualAt(e.GetPosition(dg)) is not Control targetControl
            || targetControl.DataContext is not TutorialSentence targetItem)
        {
            return false;
        }

        var items = vm.Content;
        return RunDropAction(dg, e, execute, sourceItem, targetItem, items);
    }
}