using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Styling;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Extensions;
using ClassIsland.Services;
using ClassIsland.ViewModels;
using Microsoft.Extensions.Logging;
namespace ClassIsland.Controls;

/// <summary>
/// WeekOffsetSettingsControl 的交互逻辑。<br/>
/// 注意：要完整加载此控件，需调用 <see cref="InitializeAfterLoad"/> 方法。<br/>
/// 外部 Flyout 请订阅 <see cref="CloseRequested"/> 事件。
/// </summary>
public partial class WeekOffsetSettingsControl : UserControl
{
    public event EventHandler CloseRequested;
    
    /// <summary>
    /// 更新 <see cref="WeekOffsetSettingsControl"/> 中的数据。
    /// </summary>
    public void InitializeAfterLoad()
    {
        UpdateIndexes();
        GenerateWeekContentPanel();
    }

    void UpdateIndexes() => ViewModel.CyclePositionIndexes =
            new(LessonsService.GetCyclePositionsByDate().Select(x => x - 1));
    
    async Task GenerateWeekContentPanel()
    {
         if (ContentPanel.Children.Count == SettingsService.Settings.MultiWeekRotationMaxCycle - 1) return;
         
         ContentPanel.Children.Clear();
         for (var week = 2; week <= SettingsService.Settings.MultiWeekRotationMaxCycle; week++)
         {
             var weekSelectorPanel = CreateWeekSelectorPanel(week);
             ContentPanel.Children.Add(weekSelectorPanel);
         }
         
         await Task.Delay(1);
         await Task.Delay(1);
         await Task.Delay(1); // 歇会
         ContentScrollViewer.Offset = new Vector(0, 0);
    }
    

    StackPanel CreateWeekSelectorPanel(int cycleWeeks) =>
        new()
        {
            Margin = new Thickness(0, 8, 0, 0),
            Spacing = 4,
            Children =
            {
                new TextBlock { Text = cycleWeeks == 2 ? "单双周：" : $"{cycleWeeks.ToChinese()}周轮换：" },
                new ListBox
                {
                    ItemsSource = CreateWeekItems(cycleWeeks),
                    MaxWidth = 400,
                    Theme = ChipListBoxTheme,
                    [!SelectingItemsControl.SelectedIndexProperty] =
                        new Binding($"{nameof(ViewModel.CyclePositionIndexes)}[{cycleWeeks}]")
                    {
                        Source = ViewModel,
                        Mode = BindingMode.TwoWay
                    }
                }
            }
        };

    static ObservableCollection<string> CreateWeekItems(int cycleWeeks)
    {
        if (cycleWeeks == 2)
            return ["单周", "双周"];
        return new ObservableCollection<string>(
            Enumerable.Range(1, cycleWeeks)
                      .Select(i => $"{i}/{cycleWeeks}周")
        );
    }

    private void ButtonFinish_OnClick(object _, RoutedEventArgs e)
    {
        CloseRequested.Invoke(this, EventArgs.Empty);
        SetCyclePositionsOffset();
    }

    // 对称逻辑：LessonsService.GetCyclePositionsByDate(today)
    private void SetCyclePositionsOffset()
    {
        var cyclePositionOffset = new ObservableCollection<int>([-1, -1]);
        var totalElapsedWeeks = (int)Math.Floor((ExactTimeService.GetCurrentLocalDateTime().Date - SettingsService.Settings.SingleWeekStartTime).TotalDays / 7);

        for (var cycleLength = 2; cycleLength <= SettingsService.Settings.MultiWeekRotationMaxCycle; cycleLength++)
        {
            var cycleOffset = (ViewModel.CyclePositionIndexes.GetValueOrDefault(cycleLength) - totalElapsedWeeks) % cycleLength;
            if (cycleOffset < 0) cycleOffset += cycleLength;
            cyclePositionOffset.Add(cycleOffset);
        }

        SettingsService.Settings.MultiWeekRotationOffset = cyclePositionOffset;
    }

    private void ButtonClear_OnClick(object _, RoutedEventArgs e)
    {
        SettingsService.Settings.MultiWeekRotationOffset.Clear();
        UpdateIndexes();
    }
    private IExactTimeService ExactTimeService { get; } = App.GetService<IExactTimeService>();
    private ILessonsService LessonsService { get; } = App.GetService<ILessonsService>();
    private ILogger<WeekOffsetSettingsControl> Logger { get; } = App.GetService<ILogger<WeekOffsetSettingsControl>>();
    private SettingsService SettingsService { get; } = App.GetService<SettingsService>();
    private WeekOffsetSettingsControlViewModel ViewModel { get; } = new();
    private ControlTheme ChipListBoxTheme { get; } = (ControlTheme)Application.Current.FindResource("ChipListBoxTheme");
    public WeekOffsetSettingsControl() => InitializeComponent();
}