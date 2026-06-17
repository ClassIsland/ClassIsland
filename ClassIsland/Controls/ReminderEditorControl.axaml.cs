using System;
using System.Globalization;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Models.Ruleset;
using ClassIsland.Shared.Models.Automation;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls;

public partial class ReminderEditorControl : UserControl
{
    public Reminder Editing { get; private set; } = new Reminder();
    private bool _isLoading;

    /// <summary>
    /// 编辑器中任意值变更时触发，用于实现实时刷新。
    /// </summary>
    public event EventHandler? EditingChanged;

    private void OnEditingChanged()
    {
        if (!_isLoading)
            EditingChanged?.Invoke(this, EventArgs.Empty);
    }

    public ReminderEditorControl()
    {
        InitializeComponent();
        FrequencyCombo.SelectionChanged += FrequencyCombo_SelectionChanged;

        // 订阅所有控件变更事件，实现编辑即刷新
        TitleBox.TextChanged += (_, _) => OnEditingChanged();
        MessageBox.TextChanged += (_, _) => OnEditingChanged();
        FrequencyCombo.SelectionChanged += (_, _) => OnEditingChanged();

        OnceDatePicker.SelectedDateChanged += (_, _) => OnEditingChanged();
        OnceTimeBox.SelectedTimeChanged += (_, _) => OnEditingChanged();

        DailyTimeBox.SelectedTimeChanged += (_, _) => OnEditingChanged();
        DailyStartDate.SelectedDateChanged += (_, _) => OnEditingChanged();
        DailyEndDate.SelectedDateChanged += (_, _) => OnEditingChanged();

        WeeklyTimeBox.SelectedTimeChanged += (_, _) => OnEditingChanged();
        ChkSun.IsCheckedChanged += (_, _) => OnEditingChanged();
        ChkMon.IsCheckedChanged += (_, _) => OnEditingChanged();
        ChkTue.IsCheckedChanged += (_, _) => OnEditingChanged();
        ChkWed.IsCheckedChanged += (_, _) => OnEditingChanged();
        ChkThu.IsCheckedChanged += (_, _) => OnEditingChanged();
        ChkFri.IsCheckedChanged += (_, _) => OnEditingChanged();
        ChkSat.IsCheckedChanged += (_, _) => OnEditingChanged();
        WeeklyStartDate.SelectedDateChanged += (_, _) => OnEditingChanged();
        WeeklyEndDate.SelectedDateChanged += (_, _) => OnEditingChanged();

        YearlyTimeBox.SelectedTimeChanged += (_, _) => OnEditingChanged();
        YearMonthCombo.SelectionChanged += (_, _) => OnEditingChanged();
        YearDayBox.TextChanged += (_, _) => OnEditingChanged();
        YearlyStartDate.SelectedDateChanged += (_, _) => OnEditingChanged();
        YearlyEndDate.SelectedDateChanged += (_, _) => OnEditingChanged();

        EnabledSwitch.IsCheckedChanged += (_, _) => OnEditingChanged();
        ConditionEnabledSwitch.IsCheckedChanged += (_, _) =>
        {
            ConditionRulesetEditor.IsVisible = ConditionEnabledSwitch.IsChecked == true;
            OnEditingChanged();
        };
    }

    private void FrequencyCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdatePanels();
    }

    private void UpdatePanels()
    {
        var idx = FrequencyCombo.SelectedIndex;
        OncePanel.IsVisible = idx == 0;
        DailyPanel.IsVisible = idx == 1;
        WeeklyPanel.IsVisible = idx == 2;
        YearlyPanel.IsVisible = idx == 3;
    }

    public void LoadFrom(Reminder r)
    {
        if (r == null) return;
        _isLoading = true;
        Editing = r;
        TitleBox.Text = r.Title;
        MessageBox.Text = r.Message;
        // frequency
        FrequencyCombo.SelectedIndex = r.Frequency switch
        {
            ReminderFrequency.Once => 0,
            ReminderFrequency.Daily => 1,
            ReminderFrequency.Weekly => 2,
            ReminderFrequency.Yearly => 3,
            _ => 0
        };

        OnceDatePicker.SelectedDate = r.Time.Date;
        OnceTimeBox.SelectedTime = r.Time.TimeOfDay;

        DailyTimeBox.SelectedTime = r.TimeOfDay;
        DailyStartDate.SelectedDate = r.StartDate;
        DailyEndDate.SelectedDate = r.EndDate;

        WeeklyTimeBox.SelectedTime = r.TimeOfDay;
        ChkSun.IsChecked = r.WeekDays.HasFlag(ReminderWeekDays.Sunday);
        ChkMon.IsChecked = r.WeekDays.HasFlag(ReminderWeekDays.Monday);
        ChkTue.IsChecked = r.WeekDays.HasFlag(ReminderWeekDays.Tuesday);
        ChkWed.IsChecked = r.WeekDays.HasFlag(ReminderWeekDays.Wednesday);
        ChkThu.IsChecked = r.WeekDays.HasFlag(ReminderWeekDays.Thursday);
        ChkFri.IsChecked = r.WeekDays.HasFlag(ReminderWeekDays.Friday);
        ChkSat.IsChecked = r.WeekDays.HasFlag(ReminderWeekDays.Saturday);
        WeeklyStartDate.SelectedDate = r.StartDate;
        WeeklyEndDate.SelectedDate = r.EndDate;

        YearlyTimeBox.SelectedTime = r.TimeOfDay;
        if (r.YearMonth >= 1 && r.YearMonth <= 12) YearMonthCombo.SelectedIndex = r.YearMonth - 1;
        YearDayBox.Text = r.YearDay > 0 ? r.YearDay.ToString() : r.Time.Day.ToString();
        YearlyStartDate.SelectedDate = r.StartDate;
        YearlyEndDate.SelectedDate = r.EndDate;

        EnabledSwitch.IsChecked = r.IsEnabled;

        // 初始化 ActionSet（确保 ActionControl 有绑定的行动组）
        Editing.ActionSet ??= new ActionSet();
        ActionSetEditor.ActionSet = Editing.ActionSet;

        // 初始化条件判断
        ConditionEnabledSwitch.IsChecked = r.IsConditionEnabled;
        ConditionRulesetEditor.IsVisible = r.IsConditionEnabled;
        if (r.ConditionSettings != null)
        {
            try
            {
                var json = JsonSerializer.Serialize(r.ConditionSettings);
                var ruleset = JsonSerializer.Deserialize<Ruleset>(json);
                if (ruleset != null)
                    ConditionRulesetEditor.Ruleset = ruleset;
                else
                    ConditionRulesetEditor.Ruleset = new Ruleset();
            }
            catch
            {
                ConditionRulesetEditor.Ruleset = new Ruleset();
            }
        }
        else
        {
            ConditionRulesetEditor.Ruleset = new Ruleset();
        }

        UpdatePanels();
        _isLoading = false;
    }

    public bool ApplyTo(Reminder r)
    {
        if (r == null) return false;
        r.Title = TitleBox.Text ?? "";
        r.Message = MessageBox.Text ?? "";

        switch (FrequencyCombo.SelectedIndex)
        {
            case 0: ApplyOnce(r); break;
            case 1: ApplyDaily(r); break;
            case 2: ApplyWeekly(r); break;
            case 3: ApplyYearly(r); break;
        }

        // 由用户手动控制启用/禁用，不再自动禁用
        r.IsEnabled = EnabledSwitch.IsChecked ?? true;

        // 保存条件判断设置
        r.IsConditionEnabled = ConditionEnabledSwitch.IsChecked ?? false;
        try
        {
            r.ConditionSettings = JsonSerializer.SerializeToElement(ConditionRulesetEditor.Ruleset);
        }
        catch
        {
            r.ConditionSettings = null;
        }

        // 仍然计算下一次发生时间，但不强制修改 IsEnabled
        var next = r.GetNextOccurrence(DateTime.Now);
        if (next.HasValue)
        {
            r.Time = next.Value;
        }

        return true;
    }

    private void ApplyOnce(Reminder r)
    {
        r.Frequency = ReminderFrequency.Once;
        if (OnceDatePicker.SelectedDate.HasValue)
        {
            r.Time = OnceDatePicker.SelectedDate.Value.Date + OnceTimeBox.SelectedTime.GetValueOrDefault();
            r.TimeOfDay = OnceTimeBox.SelectedTime.GetValueOrDefault();
        }
    }

    private void ApplyDaily(Reminder r)
    {
        r.Frequency = ReminderFrequency.Daily;
        r.TimeOfDay = DailyTimeBox.SelectedTime.GetValueOrDefault();
        r.StartDate = DailyStartDate.SelectedDate;
        r.EndDate = DailyEndDate.SelectedDate;
    }

    private void ApplyWeekly(Reminder r)
    {
        r.Frequency = ReminderFrequency.Weekly;
        r.TimeOfDay = WeeklyTimeBox.SelectedTime.GetValueOrDefault();
        var wd = ReminderWeekDays.None;
        if (ChkSun.IsChecked == true) wd |= ReminderWeekDays.Sunday;
        if (ChkMon.IsChecked == true) wd |= ReminderWeekDays.Monday;
        if (ChkTue.IsChecked == true) wd |= ReminderWeekDays.Tuesday;
        if (ChkWed.IsChecked == true) wd |= ReminderWeekDays.Wednesday;
        if (ChkThu.IsChecked == true) wd |= ReminderWeekDays.Thursday;
        if (ChkFri.IsChecked == true) wd |= ReminderWeekDays.Friday;
        if (ChkSat.IsChecked == true) wd |= ReminderWeekDays.Saturday;
        r.WeekDays = wd;
        r.StartDate = WeeklyStartDate.SelectedDate;
        r.EndDate = WeeklyEndDate.SelectedDate;
    }

    private void ApplyYearly(Reminder r)
    {
        r.Frequency = ReminderFrequency.Yearly;
        r.TimeOfDay = YearlyTimeBox.SelectedTime.GetValueOrDefault();
        if (YearMonthCombo.SelectedIndex >= 0) r.YearMonth = YearMonthCombo.SelectedIndex + 1;
        if (int.TryParse(YearDayBox.Text, out var yd)) r.YearDay = yd;
        r.StartDate = YearlyStartDate.SelectedDate;
        r.EndDate = YearlyEndDate.SelectedDate;
    }
}
