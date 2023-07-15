using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ClassIsland.Models;
using ClassIsland.ViewModels;
using ClassIsland.Views;

namespace ClassIsland;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainViewModel ViewModel
    {
        get;
        set;
    } = new();

    public ProfileSettingsWindow? ProfileSettingsWindow
    {
        get;
        set;
    }

    public SettingsWindow? SettingsWindow
    {
        get;
        set;
    }

    public DispatcherTimer UpdateTimer
    {
        get;
    } = new()
    {
        Interval = TimeSpan.FromMilliseconds(25)
    };

    public MainWindow()
    {
        InitializeComponent();
        UpdateTimer.Tick += UpdateTimerOnTick;
        DataContext = this;
        UpdateTimer.Start();
    }

    private int GetSubjectIndex(int index)
    {
        var k = ViewModel.CurrentClassPlan?.TimeLayout.Layouts[index];
        var l = (from t in ViewModel.CurrentClassPlan?.TimeLayout.Layouts where t.TimeType == 0 select t).ToList();
        var i = l.IndexOf(k);
        return i;
    }

    private Storyboard BeginStoryboard(string name)
    {
        var a = (Storyboard)FindResource(name);
        a.Begin();
        return a;
    }

    private async void UpdateTimerOnTick(object? sender, EventArgs e)
    {
        LoadCurrentClassPlan();

        if (ViewModel.CurrentClassPlan is null || ViewModel.CurrentClassPlan.TimeLayout is null)
        {
            return;
        }

        var isLessonConfirmed = false;
        foreach (var i in ViewModel.CurrentClassPlan.TimeLayout.Layouts)
        {
            if (i.StartSecond.TimeOfDay <= DateTime.Now.TimeOfDay && i.EndSecond.TimeOfDay >= DateTime.Now.TimeOfDay)
            {
                ViewModel.CurrentSelectedIndex = ViewModel.CurrentClassPlan.TimeLayout.Layouts.IndexOf(i);
                isLessonConfirmed = true;
                break;
            }
        }

        if (!isLessonConfirmed)
        {
            ViewModel.CurrentSelectedIndex = null;
        }
        else if (ViewModel.CurrentSelectedIndex + 1 < ViewModel.CurrentClassPlan.TimeLayout.Layouts.Count && ViewModel.CurrentSelectedIndex is not null)
        {
            var i0 = GetSubjectIndex((int)ViewModel.CurrentSelectedIndex + 1);
            var i1  = (int)ViewModel.CurrentSelectedIndex + 1;
            if (ViewModel.CurrentClassPlan.Classes.Count > i0 && ViewModel.CurrentClassPlan.TimeLayout.Layouts.Count > i1 && i0 >= 0)
            {
                var index = ViewModel.CurrentClassPlan.Classes[i0].SubjectId;
                ViewModel.NextSubject = ViewModel.Profile.Subjects[index];
                ViewModel.NextTimeLayoutItem = ViewModel.CurrentClassPlan.TimeLayout.Layouts[i1];
            }
        }

        var tClassDelta = ViewModel.NextTimeLayoutItem.StartSecond.TimeOfDay - DateTime.Now.TimeOfDay;
        ViewModel.OnClassLeftTime = tClassDelta;
        if (tClassDelta > TimeSpan.Zero && tClassDelta <= TimeSpan.FromSeconds(ViewModel.Settings.ClassPrepareNotifySeconds) && !ViewModel.IsOverlayOpened)
        {
            ViewModel.IsOverlayOpened = true;
            // Notify class start
            ViewModel.CurrentMaskElement = FindResource("ClassPrepareNotifyMask");
            ViewModel.CurrentOverlayElement = FindResource("ClassPrepareNotifyOverlay");

            var a1 = BeginStoryboard("OverlayMaskIn");
            await Task.Run(() => Thread.Sleep(TimeSpan.FromSeconds(5)));
            var a2 = BeginStoryboard("OverlayMaskOut");
        }

        if (tClassDelta <= TimeSpan.Zero && ViewModel.IsOverlayOpened)
        {
            // Close Notification
            ViewModel.IsOverlayOpened = false;
            var a1 = BeginStoryboard("OverlayOut");
        }
    }

    public void LoadProfile()
    {
        var json = File.ReadAllText("./Profile.json");
        var r = JsonSerializer.Deserialize<Profile>(json);
        if (r != null)
        {
            ViewModel.Profile = r;
        }
    }

    public void SaveProfile()
    {
        File.WriteAllText("./Profile.json", JsonSerializer.Serialize<Profile>(ViewModel.Profile));
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        LoadProfile();
        ViewModel.Profile.PropertyChanged += (sender, args) => SaveProfile();
    }

    private void ButtonSettings_OnClick(object sender, RoutedEventArgs e)
    {
        ProfileSettingsWindow = new ProfileSettingsWindow
        {
            MainViewModel = ViewModel,
            Owner = this
        };
        ProfileSettingsWindow.Closed += (o, args) => SaveProfile();
        ProfileSettingsWindow.Show();
    }

    public bool CheckClassPlan(ClassPlan plan)
    {
        if (plan.TimeRule.WeekDay != (int)DateTime.Now.DayOfWeek)
        {
            return false;
        }
        // TODO: 完成单双周判定
        return true;
    }

    public void LoadCurrentClassPlan()
    {
        ViewModel.Profile.RefreshTimeLayouts();
        var a = (from p in ViewModel.Profile.ClassPlans
            where CheckClassPlan(p.Value)
            select p.Value).ToList();
        ViewModel.CurrentClassPlan = a.Count < 1 ? null : a[0]!;
    }

    private void ListView_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        e.Handled = true;
    }

    private void ButtonResizeDebug_OnClick(object sender, RoutedEventArgs e)
    {
        SizeToContent = SizeToContent.WidthAndHeight;
    }

    private void MainWindow_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MenuItemSettings_OnClick(object sender, RoutedEventArgs e)
    {
        SettingsWindow = new SettingsWindow()
        {
            Owner = this,
            MainViewModel = ViewModel,
            Settings = ViewModel.Settings
        };
        SettingsWindow.ShowDialog();
    }

    private void MenuItemDebugOverlayMaskIn_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CurrentMaskElement = FindResource("ClassPrepareNotifyMask");
        ViewModel.CurrentOverlayElement = FindResource("ClassPrepareNotifyOverlay");
        var a = (Storyboard)FindResource("OverlayMaskIn");
        a.Begin();
    }

    private void MenuItemDebugOverlayMaskOut_OnClick(object sender, RoutedEventArgs e)
    {
        var a = (Storyboard)FindResource("OverlayMaskOut");
        a.Begin();
    }

    private void MenuItemDebugOverlayOut_OnClick(object sender, RoutedEventArgs e)
    {
        var a = (Storyboard)FindResource("OverlayOut");
        a.Begin();
    }
}
