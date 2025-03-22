#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Helpers.Native;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace ClassIsland.Services;

public sealed class WallpaperPickingService : IHostedService, INotifyPropertyChanged
{
    private SettingsService SettingsService { get; }

    private static readonly string DesktopWindowClassName = "Progman";
    private ObservableCollection<Color> _wallpaperColorPlatte = new();
    private BitmapImage _wallpaperImage = new();
    private bool _isWorking = false;

    public RegistryNotifier RegistryNotifier
    {
        get;
    }

    private DispatcherTimer UpdateTimer
    {
        get;
    } = new DispatcherTimer()
    {
        Interval = TimeSpan.FromMinutes(1)
    };

    private ILogger<WallpaperPickingService> Logger { get; }

    public static void ColorToHsv(System.Windows.Media.Color color, out double hue, out double saturation, out double value)
    {
        int max = Math.Max(color.R, Math.Max(color.G, color.B));
        int min = Math.Min(color.R, Math.Min(color.G, color.B));

        hue = 0;
        saturation = (max == 0) ? 0 : 1d - (1d * min / max);
        value = max / 255d;
    }

    public ObservableCollection<Color> WallpaperColorPlatte
    {
        get => _wallpaperColorPlatte;
        set
        {
            if (Equals(value, _wallpaperColorPlatte)) return;
            _wallpaperColorPlatte = value;
            OnPropertyChanged();
        }
    }

    public BitmapImage WallpaperImage
    {
        get => _wallpaperImage;
        set
        {
            if (Equals(value, _wallpaperImage)) return;
            _wallpaperImage = value;
            OnPropertyChanged();
        }
    }

    public WallpaperPickingService(SettingsService settingsService, ILogger<WallpaperPickingService> logger)
    {
        Logger = logger;
        SettingsService = settingsService;
        SystemEvents.UserPreferenceChanged += SystemEventsOnUserPreferenceChanged;
        RegistryNotifier = new RegistryNotifier(RegistryNotifier.HKEY_CURRENT_USER, "Control Panel\\Desktop");
        RegistryNotifier.RegistryKeyUpdated += RegistryNotifierOnRegistryKeyUpdated;
        RegistryNotifier.Start();
        AppBase.Current.AppStopping += (sender, args) => RegistryNotifier.Stop();
        AppBase.Current.AppStopping += (sender, args) => SystemEvents.UserPreferenceChanged -= SystemEventsOnUserPreferenceChanged;
        UpdateTimer.Tick += UpdateTimerOnTick;
        UpdateTimer.Interval = TimeSpan.FromSeconds(SettingsService.Settings.WallpaperAutoUpdateIntervalSeconds);
        SettingsService.Settings.PropertyChanged += SettingsServiceOnPropertyChanged;
        UpdateUpdateTimerEnableState();
    }

    private void SettingsServiceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SettingsService.Settings.WallpaperAutoUpdateIntervalSeconds):
                UpdateTimer.Interval = TimeSpan.FromSeconds(SettingsService.Settings.WallpaperAutoUpdateIntervalSeconds);
                break;
            case nameof(SettingsService.Settings.IsWallpaperAutoUpdateEnabled):
            case nameof(SettingsService.Settings.ColorSource):
                UpdateUpdateTimerEnableState();
                break;
        }
    }

    private void UpdateUpdateTimerEnableState()
    {
        if ((SettingsService.Settings.ColorSource == 1 && SettingsService.Settings.IsWallpaperAutoUpdateEnabled) ||
            SettingsService.Settings.ColorSource == 3)
        {
            UpdateTimer.Start();
        }
        else
        {
            UpdateTimer.Stop();
        }
    }

    private async void UpdateTimerOnTick(object? sender, EventArgs e)
    {
        Logger.LogInformation("自动提取主题色Timer触发。");
        await GetWallpaperAsync();
    }

    private async void RegistryNotifierOnRegistryKeyUpdated()
    {
        Logger.LogInformation("壁纸注册表项更新触发。");
        Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await GetWallpaperAsync();
        });
    }

    private IntPtr HwndSourceHookProcess(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        if (msg == 0x0317)
        {
            Debug.WriteLine("printed");
        }
        return default;
    }

    private async void SystemEventsOnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.Desktop)
        {
            Logger.LogInformation("UserPreferenceChanged事件更新触发。");
            //await Task.Run(=>Thread.Sleep(TimeSpan.FromSeconds(1));
            await GetWallpaperAsync();
        }
    }

    private Bitmap? GetFullScreenShot(Screen screen)
    {
        try
        {
            var baseImage = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);
            var g = Graphics.FromImage(baseImage);
            g.CopyFromScreen(new(0, 0), new(0, 0), screen.Bounds.Size);
            g.Dispose();
            return baseImage;
        } catch (Exception ex)
        {
            Logger.LogError(ex, "获取屏幕截图失败。");
            return null;
        }
    }

    public static Bitmap? GetScreenShot(string className)
    {
        var win = NativeWindowHelper.FindWindowByClass(className);
        if (win == IntPtr.Zero)
        {
            return null;
        }

        return WindowCaptureHelper.CaptureWindowBitBlt(win);
    }

    public Bitmap? GetFallbackWallpaper()
    {
        Logger.LogInformation("正在以兼容模式获取壁纸。");

        try
        {
            var k = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop");
            var path = (string?)k?.GetValue("WallPaper");
            var b = Screen.PrimaryScreen.Bounds;
            if (path == null)
                return null;
            else
            {
                var image = Image.FromFile(path);
                var m = 1.0;
                if (image.Width > image.Height)
                {
                    m = 1.0 * b.Height / image.Height;
                }
                else
                {
                    m = 1.0 * b.Width / image.Width;
                }
                return new Bitmap(image, (int)(image.Width * m), (int)(image.Height * m));
            }
        }
        catch (Exception ex) 
        {
            Logger.LogError(ex, "以兼容模式获取壁纸失败。");
            return null;
        }
    }

    public bool IsWorking
    {
        get => _isWorking;
        set
        {
            if (value == _isWorking) return;
            _isWorking = value;
            OnPropertyChanged();
        }
    }

    public async Task GetWallpaperAsync()
    {
        if (IsWorking)
        {
            return;
        }

        IsWorking = true;
        Logger.LogInformation("正在提取壁纸主题色。");

        try
        {
            await Task.Run(() =>
            {
                var bitmap = SettingsService.Settings.ColorSource == 3 ? GetFullScreenShot(SettingsService.Settings.WindowDockingMonitorIndex < Screen.AllScreens.Length && SettingsService.Settings.WindowDockingMonitorIndex >= 0 ? Screen.AllScreens[SettingsService.Settings.WindowDockingMonitorIndex] : Screen.PrimaryScreen!)
                    : SettingsService.Settings.IsFallbackModeEnabled ?
                        (GetFallbackWallpaper())
                        :
                        (GetScreenShot(
                            SettingsService.Settings.WallpaperClassName == ""
                                ? DesktopWindowClassName
                                : SettingsService.Settings.WallpaperClassName
                        ));
                if (bitmap is null)
                {
                    Logger.LogError("获取壁纸失败。");
                    return;
                }

                double dpiX = 1, dpiY = 1;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mw = (MainWindow)Application.Current.MainWindow!;
                    mw.GetCurrentDpi(out dpiX, out dpiY);
                });
                WallpaperImage = BitmapConveters.ConvertToBitmapImage(bitmap, bitmap.Width);
                var w = new Stopwatch();
                w.Start();
                var right = SettingsService.Settings.TargetLightValue - 0.5;
                var left = SettingsService.Settings.TargetLightValue + 0.5;
                var r = ColorOctTreeNode.ProcessImage(bitmap)
                    .OrderByDescending(i =>
                    {
                        var c = (Color)ColorConverter.ConvertFromString(i.Key);
                        ColorToHsv(c, out var h, out var s, out var v);
                        return (s + (v * (-(v - right) * (v - left) * 4))) * Math.Log2(i.Value);
                    })
                    .ThenByDescending(i => i.Value)
                    .ToList();
                WallpaperColorPlatte.Clear();
                for (var i = 0; i < Math.Min(r.Count, 5); i++)
                {
                    WallpaperColorPlatte.Add((Color)ColorConverter.ConvertFromString(r[i].Key));
                }
            });

            // Update cached platte
            if (SettingsService.Settings.WallpaperColorPlatte.Count < SettingsService.Settings.SelectedPlatteIndex + 1 ||
                WallpaperColorPlatte.Count < SettingsService.Settings.SelectedPlatteIndex + 1 ||
                SettingsService.Settings.SelectedPlatteIndex < 0 ||
                SettingsService.Settings.WallpaperColorPlatte[SettingsService.Settings.SelectedPlatteIndex] !=
                WallpaperColorPlatte[SettingsService.Settings.SelectedPlatteIndex])
            {
                SettingsService.Settings.WallpaperColorPlatte.Clear();
                foreach (var i in WallpaperColorPlatte)
                {
                    SettingsService.Settings.WallpaperColorPlatte.Add(i);
                }
                SettingsService.Settings.SelectedPlatteIndex = 0;
            }
        
            IsWorking = false;
            GC.Collect();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "无法提取壁纸主题色");
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return new Task(() => {});
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return new Task(() => { });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}