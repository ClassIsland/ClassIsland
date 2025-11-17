using ClassIsland.Core.Abstractions.Services;
using FluentAvalonia.UI.Windowing;
using HarmonyLib;

namespace ClassIsland.Platform.Windows.Patches;

[HarmonyPatch]
public class Win32WindowManagerConstructorPatcher
{
    [HarmonyTargetMethod]
    static System.Reflection.MethodBase TargetMethod()
    {
        var type = AccessTools.TypeByName("FluentAvalonia.UI.Windowing.Win32WindowManager");
        return AccessTools.Constructor(type, [typeof(AppWindow)]);
    }
    
    static bool Prefix(AppWindow window)
    {
        return !IThemeService.UseNativeTitlebar;
    }
}