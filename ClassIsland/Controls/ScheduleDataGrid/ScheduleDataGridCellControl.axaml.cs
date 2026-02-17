using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Shared;
using ClassIsland.Shared.ComponentModels;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls.ScheduleDataGrid;

[TemplatePart("PART_InnerListBox", typeof(ListBox))]
[TemplatePart("PART_MainTextBlock", typeof(TextBlock))]
[TemplatePart("PART_EmptyTextBlock", typeof(TextBlock))]
public class ScheduleDataGridCellControl : TemplatedControl
{
    public static readonly StyledProperty<ClassInfo> ClassInfoProperty = AvaloniaProperty.Register<ScheduleDataGridCellControl, ClassInfo>(
        nameof(ClassInfo), ClassInfo.Empty);

    public ClassInfo ClassInfo
    {
        get => GetValue(ClassInfoProperty);
        set => SetValue(ClassInfoProperty, value);
    }

    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<ScheduleDataGridCellControl, bool>(
        nameof(IsSelected));

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public static readonly StyledProperty<DateTime> DateProperty = AvaloniaProperty.Register<ScheduleDataGridCellControl, DateTime>(
        nameof(Date));

    public DateTime Date
    {
        get => GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public static readonly StyledProperty<bool> IsEditPopupOpenProperty = AvaloniaProperty.Register<ScheduleDataGridCellControl, bool>(
        nameof(IsEditPopupOpen));

    public bool IsEditPopupOpen
    {
        get => GetValue(IsEditPopupOpenProperty);
        set => SetValue(IsEditPopupOpenProperty, value);
    }

    public static readonly AttachedProperty<ObservableDictionary<Guid, Subject>> SubjectsProperty =
        AvaloniaProperty.RegisterAttached<ScheduleDataGridCellControl, Control, ObservableDictionary<Guid, Subject>>("Subjects", inherits: true);

    public static void SetSubjects(Control obj, ObservableDictionary<Guid, Subject> value) => obj.SetValue(SubjectsProperty, value);
    public static ObservableDictionary<Guid, Subject> GetSubjects(Control obj) => obj.GetValue(SubjectsProperty);

    public static readonly AttachedProperty<SyncDictionaryList<Guid, Subject>> SubjectsListProperty =
        AvaloniaProperty.RegisterAttached<ScheduleDataGridCellControl, Control, SyncDictionaryList<Guid, Subject>>("SubjectsList", inherits: true);

    public static void SetSubjectsList(Control obj, SyncDictionaryList<Guid, Subject> value) => obj.SetValue(SubjectsListProperty, value);
    public static SyncDictionaryList<Guid, Subject> GetSubjectsList(Control obj) => obj.GetValue(SubjectsListProperty);

    public static readonly RoutedEvent<ScheduleDataGridSelectionChangedEventArgs>
        ScheduleDataGridSelectionChangedEvent =
            RoutedEvent.Register<ScheduleDataGridCellControl, ScheduleDataGridSelectionChangedEventArgs>(nameof(ScheduleDataGridSelectionChanged), RoutingStrategies.Bubble);

    public event EventHandler<ScheduleDataGridSelectionChangedEventArgs> ScheduleDataGridSelectionChanged
    {
        add => AddHandler(ScheduleDataGridSelectionChangedEvent, value);
        remove => RemoveHandler(ScheduleDataGridSelectionChangedEvent, value);
    }

    public static readonly StyledProperty<bool> IsNullClassPlanProperty = AvaloniaProperty.Register<ScheduleDataGridCellControl, bool>(
        nameof(IsNullClassPlan));

    public bool IsNullClassPlan
    {
        get => GetValue(IsNullClassPlanProperty);
        set => SetValue(IsNullClassPlanProperty, value);
    }

    private IDisposable? _isSelectedPropertyObserver;
    private ListBox? _innerListBox;
    private TextBlock? _mainTextBlock;
    private TextBlock? _emptyTextBlock;
    private ScheduleDataGrid? _scheduleDataGrid;

    public ScheduleDataGridCellControl()
    {
        AddHandler(DoubleTappedEvent, OnDoubleTapped);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_innerListBox != null)
        {
            _innerListBox.Tapped -= InnerListBoxOnTapped;
        }
        base.OnApplyTemplate(e);

        _innerListBox = e.NameScope.Find<ListBox>("PART_InnerListBox");
        if (_innerListBox != null)
        {
            _innerListBox.Tapped += InnerListBoxOnTapped;
        }

        _mainTextBlock = e.NameScope.Find<TextBlock>("PART_MainTextBlock");
        _emptyTextBlock = e.NameScope.Find<TextBlock>("PART_EmptyTextBlock");
    }

    private void InnerListBoxOnTapped(object? sender, TappedEventArgs e)
    {
        IAppHost.GetService<ITutorialService>().PushToNextSentenceByTag("classisland.sdg.cell.edit.complete_edited");
        if (e.Source is Visual source && source.FindAncestorOfType<ListBoxItem>() != null)
        {
            IsEditPopupOpen = false;
            e.Handled = true;
        }
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_scheduleDataGrid?.IsReadonly == true)
        {
            return;
        }
        if (sender is Control c && c.FindAncestorOfType<ListBox>() != null)
        {
            return;
        }
        Console.WriteLine("begin edit");
        IAppHost.GetService<ITutorialService>().PushToNextSentenceByTag("classisland.sdg.cell.edit.open");
        if (ClassInfo != ClassInfo.Empty && ClassInfo != null)
        {
            IsEditPopupOpen = true;
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _isSelectedPropertyObserver ??= this.GetObservable(IsSelectedProperty)
            .Skip(1)
            .Subscribe(_ =>
            {
                if (!IsSelected)
                {
                    return;
                }
                RaiseEvent(new ScheduleDataGridSelectionChangedEventArgs(ScheduleDataGridSelectionChangedEvent)
                {
                    ClassInfo = ClassInfo,
                    Date = Date
                });
            });
        _scheduleDataGrid = this.FindAncestorOfType<ScheduleDataGrid>();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _isSelectedPropertyObserver?.Dispose();
        _isSelectedPropertyObserver = null;
        _scheduleDataGrid = null;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
    }
}