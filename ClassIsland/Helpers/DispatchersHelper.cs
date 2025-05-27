using System;
using Avalonia.Threading;
using ClassIsland.Core;

namespace ClassIsland.Helpers;

public static class DispatchersHelper
{
    public static void InvokeOnAvalonia(Action action)
    {
        Dispatcher.UIThread.Invoke(action);
    }

    public static void InvokeOnWpf(Action action)
    {
        AppBase.Current.Dispatcher.Invoke(action);
    }
}