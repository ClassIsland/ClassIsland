using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Helpers;
using ClassIsland.Models.ComponentSettings;
using ClassIsland.Shared;

namespace ClassIsland.Controls.Components;

/// <summary>
/// SlideComponent.xaml 的交互逻辑
/// </summary>
[ContainerComponent]
[ComponentInfo("7E19A113-D281-4F33-970A-834A0B78B5AD", "轮播容器", "\uefc9", "轮播多个组件。")]
public partial class SlideComponent : ComponentBase<SlideComponentSettings>
{
    public IRulesetService RulesetService { get; } = IAppHost.GetService<IRulesetService>();

    private int _selectedIndex = 0;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            _selectedIndex = value;
            MainListBox.SelectedIndex = value;
        }
    }

    public static readonly AttachedProperty<bool> IsAnimationEnabledProperty =
        AvaloniaProperty.RegisterAttached<SlideComponent, Control, bool>("IsAnimationEnabled", inherits: true);

    public static void SetIsAnimationEnabled(Control obj, bool value) => obj.SetValue(IsAnimationEnabledProperty, value);
    public static bool GetIsAnimationEnabled(Control obj) => obj.GetValue(IsAnimationEnabledProperty);

    private Queue<int> _randomPlaylist = [];

    private int _playingDirection = 1;

    private DispatcherTimer Timer { get; } = new()
    {
        Interval = TimeSpan.FromSeconds(5)
    };

    public SlideComponent()
    {
        InitializeComponent();
    }

    private void LoadSettings()
    {
        //Timer.Stop();
        Timer.Interval = TimeSpanHelper.FromSecondsSafe(Settings.SlideSeconds);
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
        GridRoot.DataContext = this;
        SelectedIndex = 0;
        LoadSettings();
        Settings.PropertyChanged += OnSettingsOnPropertyChanged;
        Settings.Children.CollectionChanged += ChildrenOnCollectionChanged;
        RulesetService.StatusUpdated += RulesetServiceOnStatusUpdated;
        Timer.Start();
        Timer.Tick -= TimerOnTick;  // 防止重复触发
        Timer.Tick += TimerOnTick;
    }

    private void OnSettingsOnPropertyChanged(object? o, PropertyChangedEventArgs args)
    {
        LoadSettings();
    }

    private void SlideComponent_OnUnloaded(object sender, RoutedEventArgs e)
    {
        Settings.PropertyChanged -= OnSettingsOnPropertyChanged;
        Settings.Children.CollectionChanged -= ChildrenOnCollectionChanged;
        RulesetService.StatusUpdated -= RulesetServiceOnStatusUpdated;
        Timer.Stop();
        Timer.Tick -= TimerOnTick;
    }

    private void ChildrenOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Settings.Children.Count - e.NewItems?.Count <= 0)
        {
            SelectedIndex = 0;
        }
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
