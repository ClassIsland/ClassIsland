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

    private List<int> _randomPlaylist = [];

    private bool _isPlayingReversed = false;

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
        switch (Settings.SlideMode)
        {
            case 1:  // 随机
                if (Settings.Children.Count <= 0)
                {
                    break;
                }

                if (_randomPlaylist.Count <= 0)
                {
                    CreateRandomPlaylist();
                }

                var i = _randomPlaylist[0];
                _randomPlaylist.RemoveAt(0);
                SelectedIndex = i;
                break;
            case 2:  //  往复
                if (Settings.Children.Count <= 0)
                {
                    break;
                }

                if (Settings.Children.Count <= 1)
                {
                    SelectedIndex = 0;
                    break;
                }
                if (SelectedIndex + 1 >= Settings.Children.Count)
                {
                    _isPlayingReversed = true;
                }
                if (SelectedIndex - 1 < 0)
                {
                    _isPlayingReversed = false;
                }
                SelectedIndex += _isPlayingReversed ? -1 : 1;
                break;
            case 0:  // 循环
                if (SelectedIndex + 1 >= Settings.Children.Count)
                {
                    SelectedIndex = 0;
                }
                else
                {
                    SelectedIndex++;
                }
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
        _randomPlaylist.Clear();
        if (Settings.Children.Count <= 0)
        {
            return;
        }

        Collection<int> list = [];
        for (var i = 0; i < Settings.Children.Count; i++)
        {
            list.Add(i);
        }

        while (list.Count > 0)
        {
            var i = list[Random.Shared.Next(0, list.Count - 1)];
            list.RemoveAt(Random.Shared.Next(0, list.Count - 1));
            _randomPlaylist.Add(i);
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