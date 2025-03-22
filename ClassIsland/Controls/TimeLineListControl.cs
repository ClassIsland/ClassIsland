using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClassIsland.Models.EventArgs;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Controls;

/// <summary>
/// 按照步骤 1a 或 1b 操作，然后执行步骤 2 以在 XAML 文件中使用此自定义控件。
///
/// 步骤 1a) 在当前项目中存在的 XAML 文件中使用该自定义控件。
/// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
/// 元素中:
///
///     xmlns:MyNamespace="clr-namespace:ClassIsland.Controls"
///
///
/// 步骤 1b) 在其他项目中存在的 XAML 文件中使用该自定义控件。
/// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
/// 元素中:
///
///     xmlns:MyNamespace="clr-namespace:ClassIsland.Controls;assembly=ClassIsland.Controls"
///
/// 您还需要添加一个从 XAML 文件所在的项目到此项目的项目引用，
/// 并重新生成以避免编译错误:
///
///     在解决方案资源管理器中右击目标项目，然后依次单击
///     “添加引用”->“项目”->[浏览查找并选择此项目]
///
///
/// 步骤 2)
/// 继续操作并在 XAML 文件中使用控件。
///
///     <MyNamespace:TimeLineListControl/>
///
/// </summary>
public class TimeLineListControl : ListBox
{
    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
        nameof(Scale), typeof(double), typeof(TimeLineListControl), new PropertyMetadata(1.0));

    public double Scale
    {
        get { return (double)GetValue(ScaleProperty); }
        set { SetValue(ScaleProperty, value); }
    }

    private static double BaseTicks { get; } = 1000000000.0;

    static TimeLineListControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeLineListControl), new FrameworkPropertyMetadata(typeof(TimeLineListControl)));
    }

    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(TimeLineListControl), new PropertyMetadata(default(bool)));

    public bool IsReadOnly
    {
        get { return (bool)GetValue(IsReadOnlyProperty); }
        set { SetValue(IsReadOnlyProperty, value); }
    }

    public static readonly DependencyProperty IsPanningModeEnabledProperty = DependencyProperty.Register(
        nameof(IsPanningModeEnabled), typeof(bool), typeof(TimeLineListControl), new PropertyMetadata(false));

    public bool IsPanningModeEnabled
    {
        get { return (bool)GetValue(IsPanningModeEnabledProperty); }
        set { SetValue(IsPanningModeEnabledProperty, value); }
    }

    public static readonly DependencyProperty IsStickyProperty = DependencyProperty.Register(
        nameof(IsSticky), typeof(bool), typeof(TimeLineListControl), new PropertyMetadata(default(bool)));

    public bool IsSticky
    {
        get { return (bool)GetValue(IsStickyProperty); }
        set { SetValue(IsStickyProperty, value); }
    }

    public TimeLineListControl()
    {
        AddHandler(TimeLineListItemSeparatorAdornerControl.SeparatorLikeTimePointMovedEvent, new RoutedEventHandler(SeparatorLikeTimePointMovedEventHandler));
    }

    private void SeparatorLikeTimePointMovedEventHandler(object sender, RoutedEventArgs e)
    {
        if (e is not SeparatorLikeTimePointMovedEventArgs args)
        {
            return;
        }
        if (ItemsSource is not ObservableCollection<TimeLayoutItem> layout)
        {
            return;
        }

        var rawIndex = layout.IndexOf(args.Item);
        if (rawIndex == -1)
        {
            return;
        }

        var isSorted = true;
        var timeLikeTimePoints = layout.Where(x => x.TimeType is 0 or 1 or 2).ToList();
        for (var index = 0; index < timeLikeTimePoints.Count - 1; index++)
        {
            var i = timeLikeTimePoints[index + 1];
            if (timeLikeTimePoints[index].StartSecond.TimeOfDay < timeLikeTimePoints[index + 1].StartSecond.TimeOfDay) continue;
            isSorted = false;
            break;
        }

        if (isSorted)
        {
            return;
        }

        var validTimePoints = layout.Where(x => x.TimeType is 0 or 1).ToList();
        for (var index = 0; index < validTimePoints.Count; index++)
        {
            var i = validTimePoints[index];
            if (i.StartSecond.TimeOfDay <= args.Item.StartSecond.TimeOfDay) continue;
            Console.WriteLine($"{rawIndex} -> {layout.IndexOf(i)}");
            layout.Move(rawIndex, layout.IndexOf(i));
            SelectedItem = args.Item;
            return;
        }
        layout.Move(rawIndex, layout.Count - 1);
        SelectedItem = args.Item;
    }


    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        var timeLayoutItems = (ObservableCollection<TimeLayoutItem>?)newValue;
        if (timeLayoutItems == null || timeLayoutItems.Count <= 0)
            return;
        ScrollIntoView(timeLayoutItems[0]);
        SelectedIndex = 0;
        base.OnItemsSourceChanged(oldValue, newValue);
    }
}