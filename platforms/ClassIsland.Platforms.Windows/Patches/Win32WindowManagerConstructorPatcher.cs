using ClassIsland.Core.Abstractions.Services;
using FluentAvalonia.UI.Windowing;
using HarmonyLib;

namespace ClassIsland.Platform.Windows.Patches;

// FIXME: 这里应该考虑和上游协商，添加对系统标题栏的支持
// 但考虑到一时无法促成此事，只能先这样了。
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