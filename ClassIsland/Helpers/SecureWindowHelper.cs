using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Win32.System.StationsAndDesktops;
using ClassIsland.Core;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Core.Helpers.Native;
using Pastel;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Helpers;

public static class SecureWindowHelper
{
    public const string SecureDesktopName = "ClassIsland.SecureDesktop";
    public static unsafe HDESK GetSecureDesktop()
    {
        // 创建安全桌面的代码参考了 KeePass 的实现
        var name = SecureDesktopName + Guid.NewGuid();
        var uOrgThreadId = GetCurrentThreadId();
        IntPtr pOrgDesktop = GetThreadDesktop(uOrgThreadId);
        const NativeWindowHelper.DesktopFlags deskFlags = (NativeWindowHelper.DesktopFlags.CreateMenu |
                                                           NativeWindowHelper.DesktopFlags.CreateWindow |
                                                           NativeWindowHelper.DesktopFlags.ReadObjects |
                                                           NativeWindowHelper.DesktopFlags.WriteObjects |
                                                           NativeWindowHelper.DesktopFlags.SwitchDesktop |
                                                           NativeWindowHelper.DesktopFlags.HookControl);

        HDESK pReturnDesktop;
        fixed (char* pName = name)
        {
            var pDesktopOld = OpenDesktop(pName, 0x0000, false, (uint)deskFlags);
            if (pDesktopOld != nint.Zero)
            {
                pReturnDesktop = pDesktopOld;
                return pReturnDesktop;
            }
            var pNewDesktop = CreateDesktop(pName,
                null, null, 0, (uint)deskFlags, null);
            if (pNewDesktop == IntPtr.Zero)
                throw new InvalidOperationException();

            pReturnDesktop = pNewDesktop;
        }
        return pReturnDesktop;
    }

    public static async Task ShowWindowInSecureDesktopAsync<T>(Action<T> closedCallback) where T: Window
    {
        var type = typeof(T);
        var desktop = GetSecureDesktop();
        var desktopOriginal = GetThreadDesktop(GetCurrentThreadId());
        Console.WriteLine(desktop);
        var token = new CancellationTokenSource();
        var thread = new Thread(() =>
        {
            try
            {
                //Console.WriteLine(r.ToString());
                //token.CancelAfter(TimeSpan.FromSeconds(5));
                SwitchDesktop(desktop);
                var r = SetThreadDesktop(desktop);
                var dispatcher = Dispatcher.CurrentDispatcher;
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    var closed = false;
                    var dialog = new Window()
                    {
                        Background = new SolidColorBrush(Color.FromRgb(255, 0, 0))
                    };
                    token.Token.Register(() =>
                    {
                        if (!closed)
                        {
                            dispatcher.Invoke(() =>
                            {
                                dialog.Close();
                            });
                        }
                    });
                    dialog.SetValue(ShadowAssist.CacheModeProperty, null);
                    dialog.Closed += (sender, args) =>
                    {
                        closed = true;
                        Dispatcher.ExitAllFrames();
                    };
                    dialog.InvalidateVisual();
                    dialog.ShowDialog();
                });
                token.Cancel();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString().Pastel("red"));
            }
            finally
            {
                SwitchDesktop(desktopOriginal);
            }
        })
        {
            CurrentCulture = Thread.CurrentThread.CurrentCulture,
            CurrentUICulture = Thread.CurrentThread.CurrentUICulture
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Priority = ThreadPriority.Highest;
        thread.Start();
        await Task.Run(() => token.Token.WaitHandle.WaitOne(), token.Token);
        SwitchDesktop(desktopOriginal);
        CloseDesktop(desktop);

    }
}