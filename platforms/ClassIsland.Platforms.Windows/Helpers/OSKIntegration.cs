using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Media;

namespace ClassIsland.Platform.Windows.Helpers;

// https://github.com/AvaloniaUI/Avalonia/issues/10136#issuecomment-2338492804
// https://gist.github.com/tobyfirth/65c5372be2e659141c1c4b7d99e3e268
public static class OSKIntegration
{
    private static readonly Dictionary<IInputPane, TopLevel> tlMap = new();
    private static readonly Subject<(TextBox t, bool state)> keyboard = new();
    private static bool _alreadyDone;

    public static void Integrate()
    {
        if (_alreadyDone)
            return;
        _alreadyDone = true;

        Control.LoadedEvent.AddClassHandler<TopLevel>((s, e) =>
        {
            IInputPane? input = s.InputPane;
            if (input == null)
                return;

            tlMap[input] = s;
            input.StateChanged += InputPaneStateChanged;
        }, handledEventsToo: true);

        Control.UnloadedEvent.AddClassHandler<TopLevel>((s, e) =>
        {
            IInputPane? input = s.InputPane;
            if (input == null)
                return;

            input.StateChanged -= InputPaneStateChanged;
            tlMap.Remove(input);
        }, handledEventsToo: true);

        InputElement.PointerPressedEvent.AddClassHandler<TextBox>((t, e) =>
        {
            if (e.Pointer.Type == PointerType.Touch)
                keyboard.OnNext((t, true));
        }, handledEventsToo: true);

        InputElement.LostFocusEvent.AddClassHandler<TextBox>((t, _) => keyboard.OnNext((t, false)), handledEventsToo: true);

        keyboard.Throttle(TimeSpan.FromMilliseconds(100)).Subscribe(e =>
        {
            TopLevel? tl = TopLevel.GetTopLevel(e.t);
            if (tl == null)
                return;

            IntPtr hwnd = tl.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (hwnd == IntPtr.Zero)
                return;

            IInputPane? input = tl.InputPane;
            if (input == null)
                return;

            if (e.state)
            {
                if (input.State == InputPaneState.Closed)
                {
                    ToggleOSK(hwnd);
                }
            }
            else
            {
                if (input.State == InputPaneState.Open)
                {
                    ToggleOSK(hwnd);
                }
            }
        });
    }

    // Shift content from behind the osk. Could shift the entire window instead.
    //
    // Docs at https://docs.avaloniaui.net/docs/concepts/services/input-pane#occludedrect
    // say e.EndRect/inputPane.OccludedRect should be empty, but they are not.
    private static void InputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        IInputPane inputPane = (IInputPane)sender!;
        TopLevel tl = tlMap[inputPane];

        if (tl.FocusManager?.GetFocusedElement() is not TextBox ctrl)
            return;

        if (e.NewState == InputPaneState.Open)
        {
            // Get screen position of the bottom-left point of the TextBox
            PixelPoint ctrlBottomScrn = tl.PointToScreen(ctrl.Bounds.BottomLeft);
            Point ctrlBottom = ctrlBottomScrn.ToPoint(tl.RenderScaling);

            // Get the screen position of the top-left point of the TopLevel
            Point tlTopCoords = tl.PointToScreen(tl.Bounds.TopLeft).ToPoint(tl.RenderScaling);

            // https://docs.avaloniaui.net/docs/concepts/services/input-pane#occludedrect
            // "Return value is in client coordinates relative to the current top level."
            // Translate osk relative bounds to "screen" bounds.
            Rect oskBounds = e.EndRect.Translate(tlTopCoords);

            bool contains = oskBounds.Contains(ctrlBottom);
            if (contains)
            {
                Point diff = oskBounds.TopLeft - ctrlBottom;
                tl.RenderTransform = new TranslateTransform(0, diff.Y);
            }
        }
        else
        {
            if (tl.RenderTransform is not null)
            {
                tl.RenderTransform = null;
            }
        }
    }

    private static void ToggleOSK(IntPtr hwnd)
    {
        UIHostNoLaunch uiHostNoLaunch;
        try
        {
            uiHostNoLaunch = new();
        }
        catch(COMException e)
        {
            if ((uint)e.HResult == HRESULT.REGDB_E_CLASSNOTREG)
            {
                Process p = new()
                {
                    StartInfo = new()
                    {
                        FileName = "tabtip.exe",
                        UseShellExecute = true
                    }
                };
                p.Start();
            }
            else
                throw;

            return;
        }

        var tipInvocation = (ITipInvocation)uiHostNoLaunch;
        tipInvocation.Toggle(hwnd);
        Marshal.ReleaseComObject(uiHostNoLaunch);
    }

    [ComImport, Guid("4ce576fa-83dc-4F88-951c-9d0782b4e376")]
    private class UIHostNoLaunch { }

    [ComImport, Guid("37c994e7-432b-4834-a2f7-dce1f13b834b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITipInvocation
    {
        void Toggle(IntPtr hwnd);
    }
}