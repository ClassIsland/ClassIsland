using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Extensions;
using ClassIsland.Services;
using ClassIsland.ViewModels;
using Microsoft.Extensions.Logging;
namespace ClassIsland.Controls;

/// <summary>
/// WeekOffsetSettingsControl.xaml 的交互逻辑
/// </summary>
public partial class WeekOffsetSettingsControl
{
    readonly Style ListBoxStyle =
        (Style)Application.Current.FindResource("MaterialDesignChoiceChipPrimaryOutlineListBox");
    static IExactTimeService ExactTimeService { get; } = App.GetService<IExactTimeService>();
    static ILessonsService LessonsService { get; } = App.GetService<ILessonsService>();
    static ILogger<WeekOffsetSettingsControl> Logger { get; } = App.GetService<ILogger<WeekOffsetSettingsControl>>();
    static SettingsService SettingsService { get; } = App.GetService<SettingsService>();
    static readonly WeekOffsetSettingsControlViewModel ViewModel = new(); // 此 ViewModel 为静态。

    public WeekOffsetSettingsControl()
    {
        InitializeComponent();
        IsVisibleChanged += delegate
        {
            if (!IsVisible)
            {
                // Logger.LogTrace("WeekOffsetSettingsControl.Hide.CyclePositions: [{}]", ViewModel.CyclePositionIndexes.Select(x => x + 1));
                return;
            }
            UpdateIndexes();
            // Logger.LogTrace("WeekOffsetSettingsControl.Show.CyclePositions: [{}]", LessonsService.GetCyclePositionsByDate());
            GenerateWeekContentPanel();
        };
    }

    void UpdateIndexes() => ViewModel.CyclePositionIndexes = new(LessonsService.GetCyclePositionsByDate().Select(x => x - 1));

    void GenerateWeekContentPanel()
    {
         if (ContentPanel.Children.Count == SettingsService.Settings.MultiWeekRotationMaxCycle - 1) return;

        ContentPanel.Children.Clear();
        ContentPanel.AddChildren(Enumerable.Range(2, count: SettingsService.Settings.MultiWeekRotationMaxCycle - 1).Select(CreateWeekSelectorPanel).ToArray<UIElement>());
    }

    StackPanel CreateWeekSelectorPanel(int cycleWeeks) =>
        new StackPanel { Margin = new Thickness(0, 8, 0, 0) }
            .AddChildren(
                new TextBlock { Text = cycleWeeks == 2 ? "单双周：" : $"{cycleWeeks.ToChinese()}周轮换：" },
                new NonScrollingListBox
                {
                    Style = ListBoxStyle,
                    ItemsSource = CreateWeekItems(cycleWeeks),
                    // SelectedIndex = ViewModel.CyclePositionIndexes[cycleWeeks],
                    MaxWidth=400
                }.ApplyBinding(Selector.SelectedIndexProperty,
                    new Binding(nameof(ViewModel.CyclePositionIndexes) + $"[{cycleWeeks}]")
                    {
                        Source = ViewModel,
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    })
            );

    static ObservableCollection<string> CreateWeekItems(int cycleWeeks)
    {
        if (cycleWeeks == 2)
            return ["单周", "双周"];
        return new ObservableCollection<string>(
            Enumerable.Range(1, cycleWeeks)
                      .Select(i => $"{i}/{cycleWeeks}周")
        );
    }

    void ButtonFinish_OnClick(object _, RoutedEventArgs e) => SetCyclePositionsOffset();

    // 对称逻辑：LessonsService.GetCyclePositionsByDate(now)
    static void SetCyclePositionsOffset()
    {
        var cyclePositionOffset = new ObservableCollection<int>([-1, -1]);
        var totalElapsedWeeks = (int)Math.Floor((ExactTimeService.GetCurrentLocalDateTime().Date - SettingsService.Settings.SingleWeekStartTime).TotalDays / 7);

        for (int cycleLength = 2; cycleLength <= SettingsService.Settings.MultiWeekRotationMaxCycle; cycleLength++)
        {
            int cycleOffset = (ViewModel.CyclePositionIndexes.GetValueOrDefault(cycleLength) - totalElapsedWeeks) % cycleLength;
            if (cycleOffset < 0) cycleOffset += cycleLength;
            cyclePositionOffset.Add(cycleOffset);
        }

        SettingsService.Settings.MultiWeekRotationOffset = cyclePositionOffset;
    }

    void ButtonClear_OnClick(object _, RoutedEventArgs e)
    {
        SettingsService.Settings.MultiWeekRotationOffset.Clear();
        UpdateIndexes();
        e.Handled = true;
    }
}
