using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.ComponentModels;
using ClassIsland.Models;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;
using DynamicData.Binding;
using ReactiveUI;

namespace ClassIsland.Controls.ScheduleDataGrid;

public partial class ScheduleDataGrid : TemplatedControl
{
    public static readonly ClassPlan EmptyClassPlan = new()
    {
        Name = ""
    };
    
    public static readonly StyledProperty<DateTime> SelectedDateProperty = AvaloniaProperty.Register<ScheduleDataGrid, DateTime>(
        nameof(SelectedDate), DateTime.Now);

    public DateTime SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public static readonly StyledProperty<int> WeekIndexProperty = AvaloniaProperty.Register<ScheduleDataGrid, int>(
        nameof(WeekIndex));

    public int WeekIndex
    {
        get => GetValue(WeekIndexProperty);
        set => SetValue(WeekIndexProperty, value);
    }

    public static readonly StyledProperty<WeekClassPlanRow> SelectedWeekClassPlanRowProperty = AvaloniaProperty.Register<ScheduleDataGrid, WeekClassPlanRow>(
        nameof(SelectedWeekClassPlanRow));

    public WeekClassPlanRow SelectedWeekClassPlanRow
    {
        get => GetValue(SelectedWeekClassPlanRowProperty);
        set => SetValue(SelectedWeekClassPlanRowProperty, value);
    }

    public static readonly StyledProperty<DateTime> ScheduleWeekViewBaseDateProperty = AvaloniaProperty.Register<ScheduleDataGrid, DateTime>(
        nameof(ScheduleWeekViewBaseDate));

    public DateTime ScheduleWeekViewBaseDate
    {
        get => GetValue(ScheduleWeekViewBaseDateProperty);
        set => SetValue(ScheduleWeekViewBaseDateProperty, value);
    }

    public static readonly StyledProperty<ClassPlan?> SelectedClassPlanProperty = AvaloniaProperty.Register<ScheduleDataGrid, ClassPlan?>(
        nameof(SelectedClassPlan));

    public ClassPlan? SelectedClassPlan
    {
        get => GetValue(SelectedClassPlanProperty);
        set => SetValue(SelectedClassPlanProperty, value);
    }

    public static readonly StyledProperty<ClassInfo?> SelectedClassInfoProperty = AvaloniaProperty.Register<ScheduleDataGrid, ClassInfo?>(
        nameof(SelectedClassInfo));

    public ClassInfo? SelectedClassInfo
    {
        get => GetValue(SelectedClassInfoProperty);
        set => SetValue(SelectedClassInfoProperty, value);
    }

    public static readonly AttachedProperty<SyncDictionaryList<Guid, TimeLayout>> TimeLayoutsProperty =
        AvaloniaProperty.RegisterAttached<ScheduleDataGrid, Control, SyncDictionaryList<Guid, TimeLayout>>("TimeLayouts", inherits: true);

    public static void SetTimeLayouts(Control obj, SyncDictionaryList<Guid, TimeLayout> value) => obj.SetValue(TimeLayoutsProperty, value);
    public static SyncDictionaryList<Guid, TimeLayout> GetTimeLayouts(Control obj) => obj.GetValue(TimeLayoutsProperty);

    public static readonly StyledProperty<int> SelectedClassInfoIndexProperty = AvaloniaProperty.Register<ScheduleDataGrid, int>(
        nameof(SelectedClassInfoIndex));

    public int SelectedClassInfoIndex
    {
        get => GetValue(SelectedClassInfoIndexProperty);
        set => SetValue(SelectedClassInfoIndexProperty, value);
    }

    public static readonly StyledProperty<DateTime> SelectedClassPlanDateProperty = AvaloniaProperty.Register<ScheduleDataGrid, DateTime>(
        nameof(SelectedClassPlanDate));

    public DateTime SelectedClassPlanDate
    {
        get => GetValue(SelectedClassPlanDateProperty);
        set => SetValue(SelectedClassPlanDateProperty, value);
    }

    public static readonly AttachedProperty<Guid> SelectedNewTimeLayoutIdProperty =
        AvaloniaProperty.RegisterAttached<ScheduleDataGrid, Control, Guid>("SelectedNewTimeLayoutId", inherits: true);

    public static void SetSelectedNewTimeLayoutId(Control obj, Guid value) => obj.SetValue(SelectedNewTimeLayoutIdProperty, value);
    public static Guid GetSelectedNewTimeLayoutId(Control obj) => obj.GetValue(SelectedNewTimeLayoutIdProperty);

    public static readonly RoutedEvent<ScheduleDataGridClassPlanEventArgs>
        OpenClassPlanSettingsRequestedEvent =
            RoutedEvent.Register<ScheduleDataGrid, ScheduleDataGridClassPlanEventArgs>(nameof(OpenClassPlanSettingsRequested), RoutingStrategies.Bubble);

    public event EventHandler<ScheduleDataGridClassPlanEventArgs> OpenClassPlanSettingsRequested
    {
        add => AddHandler(OpenClassPlanSettingsRequestedEvent, value);
        remove => RemoveHandler(OpenClassPlanSettingsRequestedEvent, value);
    }
    
    
    public ObservableCollection<WeekClassPlanRow> WeekClassPlanRows { get; } = [];
    public ObservableCollection<DateTime> WeekDayDates { get; } = [default, default, default, default, default, default, default];

    public ObservableCollection<ClassPlan> ClassPlansCache { get; } = [EmptyClassPlan, EmptyClassPlan, EmptyClassPlan, EmptyClassPlan, EmptyClassPlan, EmptyClassPlan, EmptyClassPlan];

    private List<IDisposable> _updateObservers = [];
    
    public IProfileService ProfileService { get; } = IAppHost.GetService<IProfileService>(); 
    public ILessonsService LessonsService { get; } = IAppHost.GetService<ILessonsService>(); 
    public SettingsService SettingsService { get; } = IAppHost.GetService<SettingsService>(); 
    
    public ScheduleDataGrid()
    {
        AddHandler(ScheduleDataGridCellControl.ScheduleDataGridSelectionChangedEvent, DataGridWeekSchedule_OnScheduleDataGridSelectionChanged, RoutingStrategies.Bubble);
        SetValue(ScheduleDataGridCellControl.SubjectsListProperty,
            new SyncDictionaryList<Guid, Subject>(ProfileService.Profile.Subjects, Guid.NewGuid));
        SetValue(TimeLayoutsProperty,
            new SyncDictionaryList<Guid, TimeLayout>(ProfileService.Profile.TimeLayouts, Guid.NewGuid));
        this.GetObservable(SelectedDateProperty).Skip(1).Subscribe(_ => RefreshWeekScheduleRows());
        Loaded += Control_OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        UnsubscribeAllObservers();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        var prevBtn = e.NameScope.Find<Button>("PART_ButtonPreviousWeek");
        if (prevBtn != null)
        {
            prevBtn.Click += ButtonPreviousWeek_OnClick;
        }
        var nextBtn = e.NameScope.Find<Button>("PART_ButtonNextWeek");
        if (nextBtn != null)
        {
            nextBtn.Click += ButtonNextWeek_OnClick;
        }
    }

    private void DataGridWeekSchedule_OnPreparingCellForEdit(object? sender, DataGridPreparingCellForEditEventArgs e)
    {
        
    }

    private void DataGridWeekSchedule_OnBeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        
    }

    private void ButtonNextWeek_OnClick(object? sender, RoutedEventArgs e)
    {
        SelectedDate += TimeSpan.FromDays(7);
        RefreshWeekScheduleRows();
    }

    private void ButtonPreviousWeek_OnClick(object? sender, RoutedEventArgs e)
    {
        SelectedDate -= TimeSpan.FromDays(7);
        RefreshWeekScheduleRows();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        RefreshWeekScheduleRows();
    }
    
    private (DataGridCell?, int) GetDataGridSelectedCell(DataGrid dataGrid)
    {
        var currentRow = dataGrid.FindDescendantOfType<DataGridRowsPresenter>()?
            .Children.OfType<DataGridRow>()
            .FirstOrDefault(r => r.FindDescendantOfType<DataGridCellsPresenter>()?
                .Children.Any(p => p.Classes.Contains(":current")) ?? false);
        var item = currentRow?.DataContext;

        var children = currentRow?.FindDescendantOfType<DataGridCellsPresenter>()?.Children;
        var currentCell = children?.OfType<DataGridCell>().FirstOrDefault(p => p.Classes.Contains(":current"));
        return (currentCell, currentCell != null ? children?.IndexOf(currentCell) ?? 0 : 0);
    }

    public void RefreshWeekScheduleRows()
    {
        var selectedDate = SelectedDate.Date;
        var baseDate = selectedDate.AddDays(-(int)selectedDate.DayOfWeek);
        ScheduleWeekViewBaseDate = baseDate;
        List<ClassPlan?> classPlans = [];
        WeekClassPlanRows.Clear();
        var maxClasses = 0;
        for (var i = 0; i < 7; i++)
        {
            var classPlan = LessonsService.GetClassPlanByDate(baseDate.AddDays(i));
            WeekDayDates[i] = baseDate.AddDays(i);
            maxClasses = Math.Max(maxClasses, classPlan?.Classes.Count ?? 0);
            classPlans.Add(classPlan);
            ClassPlansCache[i] = classPlan ?? EmptyClassPlan;
        }

        for (var i = 0; i < maxClasses; i++)
        {
            var row = new WeekClassPlanRow()
            {
                Sunday = TryGetClassInfo(classPlans[0], i),
                Monday = TryGetClassInfo(classPlans[1], i),
                Tuesday = TryGetClassInfo(classPlans[2], i),
                Wednesday = TryGetClassInfo(classPlans[3], i),
                Thursday = TryGetClassInfo(classPlans[4], i),
                Friday = TryGetClassInfo(classPlans[5], i),
                Saturday = TryGetClassInfo(classPlans[6], i),
            };
            WeekClassPlanRows.Add(row);
        }

        WeekIndex =
            (int)Math.Ceiling((baseDate.AddDays(6) - SettingsService.Settings.SingleWeekStartTime).TotalDays / 7);
        UpdateTimePoints();
        UpdateObservers();

        return;

        ClassInfo TryGetClassInfo(ClassPlan? classPlan, int index)
        {
            if (classPlan != null && classPlan.Classes.Count > index)
            {
                return classPlan.Classes[index];
            }

            return ClassInfo.Empty;
        }
    }

    private void UpdateTimePoints()
    {
        foreach (var row in WeekClassPlanRows)
        {
            row.TimePoint = TimeLayoutItem.Empty;
        }
        var classPlan = SelectedClassPlan 
                        ?? ClassPlansCache[1] 
                        ?? ClassPlansCache.FirstOrDefault(x => x != null);
        if (classPlan == null)
        {
            return;
        }

        for (int i = 0; i < Math.Min(classPlan.Classes.Count, WeekClassPlanRows.Count); i++)
        {
            WeekClassPlanRows[i].TimePoint = classPlan.Classes[i].CurrentTimeLayoutItem;
        }
    }
    
    private void ButtonRefreshScheduleAdjustmentView_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshWeekScheduleRows();
    }

    private ClassInfo? GetClassInfoFromRow(WeekClassPlanRow row, int index)
    {
        return index switch
        {
            0 => row.Sunday,
            1 => row.Monday,
            2 => row.Tuesday,
            3 => row.Wednesday,
            4 => row.Thursday,
            5 => row.Friday,
            6 => row.Saturday,
            _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
        };
    }

    private void DataGridWeekSchedule_OnScheduleDataGridSelectionChanged(object? sender, ScheduleDataGridSelectionChangedEventArgs e)
    {
        Console.WriteLine($"sel changed, {e.Date}, {e.ClassInfo.SubjectId}");
        SelectedClassInfo = e.ClassInfo;
        SelectedClassPlanDate = e.Date;
        var index = Math.Clamp((SelectedClassPlanDate - ScheduleWeekViewBaseDate).Days, 0, 6);
        SelectedClassPlan = ClassPlansCache.Count >= 7
            ? (ClassPlansCache[index] == EmptyClassPlan ? null : ClassPlansCache[index])
            : null;
        UpdateTimePoints();
    }

    private void UpdateObservers()
    {
        UnsubscribeAllObservers();
        foreach (var classPlan in ClassPlansCache)
        {
            var observer = classPlan.Classes.ObserveCollectionChanges()
                .Subscribe(_ => RefreshWeekScheduleRows());
            _updateObservers.Add(observer);
        }
        foreach (var (_, classPlan) in ProfileService.Profile.ClassPlans)
        {
            var observer = classPlan.TimeRule.WhenAnyPropertyChanged()
                .Subscribe(x => RefreshWeekScheduleRows());
            _updateObservers.Add(observer);
        }

        var globalObserver = ProfileService.Profile.ClassPlans.ObserveCollectionChanges()
            .Subscribe(_ => RefreshWeekScheduleRows());
        _updateObservers.Add(globalObserver);
        var profileObserver = ProfileService.Profile.WhenAnyPropertyChanged()
            .Subscribe(_ => RefreshWeekScheduleRows());
        _updateObservers.Add(profileObserver);
    }

    private void UnsubscribeAllObservers()
    {
        foreach (var observer in _updateObservers)
        {
            observer.Dispose();
        }
        _updateObservers.Clear();
    }
}