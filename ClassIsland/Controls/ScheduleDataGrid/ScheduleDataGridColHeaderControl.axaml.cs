using System;
using System.Reactive.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Profile;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;

namespace ClassIsland.Controls.ScheduleDataGrid;

[TemplatePart("PART_ButtonOpenClassPlanSettings", typeof(Button))]
[TemplatePart("PART_ButtonCreateClassPlan", typeof(Button))]
public partial class ScheduleDataGridColHeaderControl : TemplatedControl
{
    public static readonly StyledProperty<ClassPlan> ClassPlanProperty = AvaloniaProperty.Register<ScheduleDataGridColHeaderControl, ClassPlan>(
        nameof(ClassPlan), ScheduleDataGrid.EmptyClassPlan);

    public ClassPlan ClassPlan
    {
        get => GetValue(ClassPlanProperty);
        set => SetValue(ClassPlanProperty, value);
    }

    public static readonly StyledProperty<DateTime> DateProperty = AvaloniaProperty.Register<ScheduleDataGridColHeaderControl, DateTime>(
        nameof(Date));

    public DateTime Date
    {
        get => GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public static readonly StyledProperty<bool> IsClassPlanEmptyProperty = AvaloniaProperty.Register<ScheduleDataGridColHeaderControl, bool>(
        nameof(IsClassPlanEmpty));

    public bool IsClassPlanEmpty
    {
        get => GetValue(IsClassPlanEmptyProperty);
        private set => SetValue(IsClassPlanEmptyProperty, value);
    }

    private IDisposable? _classPlanObserver;
    private IDisposable? _timeRuleObserver;
    private Button? _buttonOpenClassPlanSettings;
    private Button? _buttonCreateClassPlan;

    public ScheduleDataGridColHeaderViewModel ViewModel { get; } = new();

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_buttonOpenClassPlanSettings != null)
        {
            _buttonOpenClassPlanSettings.Click -= ButtonOpenClassPlanSettingsOnClick;
        }
        if (_buttonCreateClassPlan != null)
        {
            _buttonCreateClassPlan.Click -= ButtonCreateClassPlanOnClick;
        }

        _buttonOpenClassPlanSettings = e.NameScope.Find<Button>("PART_ButtonOpenClassPlanSettings");
        _buttonCreateClassPlan = e.NameScope.Find<Button>("PART_ButtonCreateClassPlan");
        
        if (_buttonOpenClassPlanSettings != null)
        {
            _buttonOpenClassPlanSettings.Click += ButtonOpenClassPlanSettingsOnClick;
        }
        if (_buttonCreateClassPlan != null)
        {
            _buttonCreateClassPlan.Click += ButtonCreateClassPlanOnClick;
        }

    }

    private void ButtonOpenClassPlanSettingsOnClick(object? sender, RoutedEventArgs e)
    {
        RaiseEvent(new ScheduleDataGridClassPlanEventArgs(ScheduleDataGrid.OpenClassPlanSettingsRequestedEvent)
        {
            ClassPlan = ClassPlan
        });
    }

    private void ButtonCreateClassPlanOnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.ClassPlanTimeRule.WeekDay = (int)Date.DayOfWeek;
        UpdateSuggestedClassPlanName();
    }

    private void UpdateSuggestedClassPlanName()
    {
        var sb = new StringBuilder();
        sb.Append(ToWeek(ViewModel.ClassPlanTimeRule.WeekDay));

        if (ViewModel.ClassPlanTimeRule.WeekCountDiv > 0)
        {
            sb.Append(' ');
            if (ViewModel.ClassPlanTimeRule.WeekCountDivTotal == 2)
            {
                sb.Append(ViewModel.ClassPlanTimeRule.WeekCountDiv == 1 ? "单周" : "双周");
            }
            else
            {
                sb.Append(
                    $"{ViewModel.ClassPlanTimeRule.WeekCountDiv}/{ViewModel.ClassPlanTimeRule.WeekCountDivTotal}周");
            }
        }

        ViewModel.SuggestedClassPlanName = sb.ToString();
        
        return;

        static string ToWeek(int index) => index switch
        {
            0 => "周日",
            1 => "周一",
            2 => "周二",
            3 => "周三",
            4 => "周四",
            5 => "周五",
            6 => "周六",
            _ => "???"
        };
    }

    [RelayCommand]
    private void CompleteCreateClassPlan(object o)
    {
        var profileService = IAppHost.GetService<IProfileService>();
        if (ViewModel.ClassPlanTimeLayoutId == Guid.Empty ||
            !profileService.Profile.TimeLayouts.ContainsKey(ViewModel.ClassPlanTimeLayoutId))
        {
            this.ShowWarningToast("请选择一个有效的时间表");
            return;
        }

        var cp = new ClassPlan()
        {
            Name = string.IsNullOrEmpty(ViewModel.ClassPlanName)
                ? ViewModel.SuggestedClassPlanName
                : ViewModel.ClassPlanName,
            TimeLayoutId = ViewModel.ClassPlanTimeLayoutId,
            AssociatedGroup = profileService.Profile.SelectedClassPlanGroupId,
            TimeRule = ViewModel.ClassPlanTimeRule
        };
        profileService.Profile.ClassPlans.Add(Guid.NewGuid(), cp);
        FlyoutHelper.CloseAncestorFlyout(o);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _classPlanObserver ??= this.GetObservable(ClassPlanProperty)
            .Subscribe(x => IsClassPlanEmpty = ClassPlan == ScheduleDataGrid.EmptyClassPlan);
        _timeRuleObserver ??= ViewModel.ClassPlanTimeRule.WhenAnyPropertyChanged()
            .Subscribe(x => UpdateSuggestedClassPlanName());
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _classPlanObserver?.Dispose();
        _classPlanObserver = null;
        _timeRuleObserver?.Dispose();
        _timeRuleObserver = null;
    }
}