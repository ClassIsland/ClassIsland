using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ClassIsland.Models;
using ClassIsland.ViewModels;
using ClassIsland.Views;
using HandyControl.Tools;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Path = System.IO.Path;

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
    } = new(DispatcherPriority.Render)
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

    private void UpdateMouseStatus()
    {
        NativeWindowHelper.GetCursorPos(out var ptr);
        GetCurrentDpi(out var dpiX, out var dpiY);
        //Debug.WriteLine($"Window: {Left * dpiX} {Top * dpiY};; Cursor: {ptr.X} {ptr.Y} ;; dpi: {dpiX}");
        var cx = Left * dpiX;
        var cy = Top * dpiY;
        var cw = Width * dpiX;
        var ch = Height * dpiY;
        var cr = cx + cw;
        var cb = cy + ch;

        ViewModel.IsMouseIn = (cx <= ptr.X && cy <= ptr.Y && ptr.X <= cr && ptr.Y <= cb);
        
    }

    private async void UpdateTimerOnTick(object? sender, EventArgs e)
    {
        UpdateWindowPos();
        UpdateMouseStatus();
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

            if (ViewModel.Settings.IsClassPrepareNotificationEnabled)
            {
                var a1 = BeginStoryboard("OverlayMaskIn");
                await Task.Run(() => Thread.Sleep(TimeSpan.FromSeconds(5)));
                var a2 = BeginStoryboard("OverlayMaskOut");
            }
        }

        if (tClassDelta <= TimeSpan.Zero)
        {
            // Close Notification
            if (ViewModel.Settings.IsClassChangingNotificationEnabled && ViewModel.CurrentMaskElement != FindResource("ClassOnNotification"))
            {
                ViewModel.CurrentMaskElement = FindResource("ClassOnNotification");
                BeginStoryboard("OverlayMaskIn");
                await Task.Run(() => Thread.Sleep(TimeSpan.FromSeconds(5)));
                ViewModel.IsOverlayOpened = false;
                BeginStoryboard("OverlayMaskOutDirect");
            }
            else if (ViewModel.IsOverlayOpened )
            {
                ViewModel.IsOverlayOpened = false;
                var a1 = BeginStoryboard("OverlayOut");
            }
        }

        // Finished update
        ViewModel.Today = DateTime.Now;
        MainListBox.SelectedIndex = ViewModel.CurrentSelectedIndex ?? -1;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        UpdateTheme();
        base.OnContentRendered(e);
    }

    public void LoadProfile()
    {
        if (!File.Exists("./Profile.json"))
        {
            return;
        }
        var json = File.ReadAllText("./Profile.json");
        var r = JsonSerializer.Deserialize<Profile>(json);
        if (r != null)
        {
            ViewModel.Profile = r;
            ViewModel.Profile.PropertyChanged += (sender, args) => SaveProfile();
        }
    }

    public void SaveProfile()
    {
        File.WriteAllText("./Profile.json", JsonSerializer.Serialize<Profile>(ViewModel.Profile));
    }

    private void LoadSettings()
    {
        if (!File.Exists("./Settings.json"))
        {
            return;
        }
        var json = File.ReadAllText("./Settings.json");
        var r = JsonSerializer.Deserialize<Settings>(json);
        if (r != null)
        {
            ViewModel.Settings = r;
            ViewModel.Settings.PropertyChanged += (sender, args) => SaveSettings();
        }
    }

    public void SaveSettings()
    {
        UpdateTheme();
        File.WriteAllText("./Settings.json", JsonSerializer.Serialize<Settings>(ViewModel.Settings));
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        ViewModel.Profile.PropertyChanged += (sender, args) => SaveProfile();
        ViewModel.Settings.PropertyChanged += (sender, args) => SaveSettings();
        LoadProfile();
        LoadSettings();
        UpdateTheme();
    }

    private void SetBottom()
    {
        if (ViewModel.Settings.WindowLayer != 0)
        {
            return;
        }

        var hWnd = new WindowInteropHelper(this).Handle;
        NativeWindowHelper.SetWindowPos(hWnd, NativeWindowHelper.HWND_BOTTOM, 0, 0, 0, 0, NativeWindowHelper.SWP_NOSIZE | NativeWindowHelper.SWP_NOMOVE | NativeWindowHelper.SWP_NOACTIVATE);
    }

    private void UpdateTheme()
    {
        UpdateWindowPos();
        var hWnd = new WindowInteropHelper(this).Handle;
        var style = NativeWindowHelper.GetWindowLong(hWnd, NativeWindowHelper.GWL_EXSTYLE);
        if (!ViewModel.Settings.IsMouseClickingEnabled)
        {
            var r = NativeWindowHelper.SetWindowLong(hWnd, NativeWindowHelper.GWL_EXSTYLE, style | NativeWindowHelper.WS_EX_TRANSPARENT);
        }
        else
        {
            style &= ~(uint)NativeWindowHelper.WS_EX_TRANSPARENT;
            var r = NativeWindowHelper.SetWindowLong(hWnd, NativeWindowHelper.GWL_EXSTYLE, style);
        }

        switch (ViewModel.Settings.WindowLayer)
        {
            case 0: // bottom
                Topmost = false;
                break;
            case 1:
                Topmost = true;
                break;
        }

        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();
        var lastPrimary = theme.PrimaryMid.Color;
        var lastSecondary = theme.SecondaryMid.Color;
        var lastBaseTheme = theme.GetBaseTheme();
        switch (ViewModel.Settings.Theme)
        {
            case 0:
                try
                {
                    var key = Registry.CurrentUser.OpenSubKey(
                        "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
                    if (key != null)
                    {
                        if ((int?)key.GetValue("AppsUseLightTheme") == 0)
                        {
                            theme.SetBaseTheme(new MaterialDesignDarkTheme());
                        }
                        else
                        {
                            theme.SetBaseTheme(new MaterialDesignLightTheme());
                        }
                    }
                }
                catch
                {
                    theme.SetBaseTheme(new MaterialDesignLightTheme());
                }
                break;

            case 1:
                theme.SetBaseTheme(new MaterialDesignLightTheme());
                break;
            case 2:
                theme.SetBaseTheme(new MaterialDesignDarkTheme());
                break;
        }
        theme.SetPrimaryColor(ViewModel.Settings.PrimaryColor);
        theme.SetSecondaryColor(ViewModel.Settings.SecondaryColor);

        var lastTheme = paletteHelper.GetTheme();
        if (lastPrimary == theme.PrimaryMid.Color &&
            lastSecondary == theme.SecondaryMid.Color &&
            lastBaseTheme == theme.GetBaseTheme())
        {
            return;
        }

        paletteHelper.SetTheme(theme);
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
            e.Handled = true;
            //DragMove();
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
        SettingsWindow.Closed += (o, args) => SaveSettings();
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

    private void MenuItemDebugOverlayMaskOutDirect_OnClick(object sender, RoutedEventArgs e)
    {
        BeginStoryboard("OverlayMaskOutDirect");
    }

    private void MenuItemExitApp_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        SaveProfile();
        SaveSettings();
    }

    private void UpdateWindowPos()
    {
        GetCurrentDpi(out var dpiX, out var dpiY);

        var screen = ViewModel.Settings.WindowDockingMonitorIndex < Screen.AllScreens.Length 
            ? Screen.AllScreens[ViewModel.Settings.WindowDockingMonitorIndex] 
            : Screen.PrimaryScreen;
        var aw = RenderSize.Width * dpiX;
        var ah = RenderSize.Height * dpiY;
        var c = (double)(screen.WorkingArea.Left + screen.WorkingArea.Right) / 2;
        var ox = ViewModel.Settings.WindowDockingOffsetX;
        var oy = ViewModel.Settings.WindowDockingOffsetY;

        switch (ViewModel.Settings.WindowDockingLocation)
        {
            case 0: //左上
                Left = (screen.WorkingArea.Left + ox) / dpiX;
                Top = (screen.WorkingArea.Top + oy) / dpiY;
                break;
            case 1: // 中上
                Left = (c - aw / 2 + ox) / dpiX;
                Top = (screen.WorkingArea.Top + oy) / dpiY;
                break;
            case 2: // 右上
                Left = (screen.WorkingArea.Right - aw + ox) / dpiX;
                Top = (screen.WorkingArea.Top + oy) / dpiY;
                break;
            case 3: // 左下
                Left = (screen.WorkingArea.Left + ox) / dpiX;
                Top = (screen.WorkingArea.Bottom - ah + oy) / dpiY;
                break;
            case 4: // 中下
                Left = (c - aw / 2 + ox) / dpiX;
                Top = (screen.WorkingArea.Bottom - ah + oy) / dpiY;
                break;
            case 5: // 右下
                Left = (screen.WorkingArea.Right - aw + ox) / dpiX;
                Top = (screen.WorkingArea.Bottom - ah + oy) / dpiY;
                break;
        }
    }

    private void GetCurrentDpi(out double dpiX, out double dpiY)
    {
        var source = PresentationSource.FromVisual(this);

        dpiX = 1.0;
        dpiY = 1.0;

        if (source?.CompositionTarget != null)
        {
            dpiX = 1.0 * source.CompositionTarget.TransformToDevice.M11;
            dpiY = 1.0 * source.CompositionTarget.TransformToDevice.M22;
        }
    }

    private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateWindowPos();
    }

    private void MainWindow_OnActivated(object? sender, EventArgs e)
    {
        SetBottom();
    }

    private void MainWindow_OnStateChanged(object? sender, EventArgs e)
    {
        SetBottom();
    }
}
