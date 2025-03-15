using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Services;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Controls.Components;

/// <summary>
/// SlideComponent.xaml 的交互逻辑
/// </summary>
[ContainerComponent]
[ComponentInfo("7E19A113-D281-4F33-970A-834A0B78B5AD", "轮播组件", PackIconKind.Slideshow, "轮播多个组件。")]
public partial class SlideComponent
{
    public IRulesetService RulesetService { get; }

    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
        nameof(SelectedIndex), typeof(int), typeof(SlideComponent), new PropertyMetadata(default(int)));

    public int SelectedIndex
    {
        get { return (int)GetValue(SelectedIndexProperty); }
        set { SetValue(SelectedIndexProperty, value); }
    }

    private Queue<int> _randomPlaylist = [];

    private int _playingDirection = 1;

    private DispatcherTimer Timer { get; } = new()
    {
        Interval = TimeSpan.FromSeconds(5)
    };

    public SlideComponent(IRulesetService rulesetService)
    {
        RulesetService = rulesetService;
        InitializeComponent();
    }

    private void LoadSettings()
    {
        //Timer.Stop();
        Timer.Interval = TimeSpan.FromSeconds(Settings.SlideSeconds);
        RefreshRules();
        //Timer.Start();
    }

    private void TimerOnTick(object? sender, EventArgs e)
    {
        if (Settings.Children.Count <= 1)    // 没有或只有一个组件时不轮播
        {
            SelectedIndex = 0;
            return;
        }
        bool[] flag = new bool[Settings.Children.Count];
        int count = 0;
        // flag用于避免重复检查，count用于记录已检查的组件数
        do
        {
            // 不断尝试下一个组件直到找到一个可见的
            ShowNext();
            if (!flag[SelectedIndex]
                && Settings.Children[SelectedIndex].HideOnRule
                && RulesetService.IsRulesetSatisfied(Settings.Children[SelectedIndex].HidingRules))
            {
                flag[SelectedIndex] = true;
                count++;
            }
            if (count >= Settings.Children.Count)
            {   // 所有组件均不可见，退出循环
                break;
            }
        }
        while (flag[SelectedIndex]);
    }

    private void ShowNext()
    {
        switch (Settings.SlideMode)
        {
            case 0:  // 循环
                SelectedIndex = SelectedIndex + 1 >= Settings.Children.Count ? 0 : SelectedIndex + 1;
                break;
            case 1:  // 随机
                if (_randomPlaylist.Count <= 0)
                {
                    CreateRandomPlaylist();
                }
                SelectedIndex = _randomPlaylist.Dequeue();
                break;
            case 2:  // 往复
                int t = SelectedIndex + _playingDirection;
                if (t < 0 || t >= Settings.Children.Count)
                {    // 碰到边界时反向播放
                    _playingDirection = -_playingDirection;
                }
                SelectedIndex += _playingDirection;
                break;
        }
    }

    private void SlideComponent_OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadSettings();
        Settings.PropertyChanged += (o, args) => LoadSettings();
        Settings.Children.CollectionChanged += ChildrenOnCollectionChanged;
        RulesetService.StatusUpdated += RulesetServiceOnStatusUpdated;
        Timer.Start();
        Timer.Tick += TimerOnTick;
    }

    private void ChildrenOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CreateRandomPlaylist();
    }

    private void CreateRandomPlaylist()
    {
        if (Settings.Children.Count <= 0)
        {
            return;
        }
        _randomPlaylist.Clear();
        int[] list = new int[Settings.Children.Count];
        for (int i = 0; i < Settings.Children.Count; i++)
        {
            list[i] = i;
        }
        Random rand = new();
        rand.Shuffle(list);
        foreach (var i in list)
        {
            _randomPlaylist.Enqueue(i);
        }
    }

    private void RulesetServiceOnStatusUpdated(object? sender, EventArgs e)
    {
        RefreshRules();
    }

    private void RefreshRules()
    {
        var isStop = RulesetService.IsRulesetSatisfied(Settings.StopRule) && Settings.IsStopOnRuleEnabled;
        var isPause = RulesetService.IsRulesetSatisfied(Settings.PauseRule) && Settings.IsPauseOnRuleEnabled;
        if (isStop)
        {
            Timer.Stop();
            SelectedIndex = 0;
            return;
        }

        if (isPause)
        {
            Timer.Stop();
            return;
        }
        Timer.Start();
    }
}