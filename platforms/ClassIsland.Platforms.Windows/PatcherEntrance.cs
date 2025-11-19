using HarmonyLib;

namespace ClassIsland.Platform.Windows;

public static class PatcherEntrance
{
    public static void InstallPatchers()
    {
        var harmony = new Harmony("cn.classisland.app.patchers");
        harmony.PatchAll();
    }
}