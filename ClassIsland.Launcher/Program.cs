using System.Diagnostics;
using System.Reflection;

#if Platforms_Windows
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
#endif

var root = Path.GetFullPath(Path.GetDirectoryName(Environment.ProcessPath) ?? "");
var installation = Directory.GetDirectories(root)
    .Where(x => Path.GetFileName(x).StartsWith("app") && !File.Exists(Path.Combine(x, ".destroy")))
    .OrderBy(x => File.Exists(Path.Combine(x, ".current")) ? 1 : 0)
    .ThenBy(x =>
    {
        var filename = Path.GetFileName(x);
        var split = filename.Split('-');
        if (split.Length <= 1)
        {
            return new Version();
        }

        return Version.TryParse(split[1], out var version) ? version : new Version();
    })
    .ThenBy(x =>
    {
        var filename = Path.GetFileName(x);
        var split = filename.Split('-');
        if (split.Length <= 2)
        {
            return 0;
        }

        return int.TryParse(split[2], out var n) ? n : 0;
    })
    .FirstOrDefault();

// 获取对应平台的可执行文件路径
string executableName;
if (OperatingSystem.IsWindows())
{
    executableName = "ClassIsland.Desktop.exe";
} else if (OperatingSystem.IsLinux())
{
    executableName = "ClassIsland.Desktop";
}
else
{
    ShowError("ClassIsland 正在不支持的平台上运行，无法继续。");
    return 1;
}

if (installation == null || !File.Exists(Path.Combine(installation, executableName)))
{
    ShowError("找不到有效的 ClassIsland 版本，可能是安装已损坏。请在 https://classisland.tech/download 重新下载并安装 ClassIsland。");
    return 1;
}

var startInfo = new ProcessStartInfo()
{
    FileName = Path.Combine(Path.Combine(installation, executableName)),
    WorkingDirectory = root,
    EnvironmentVariables =
    {
        {"ClassIsland_PackageRoot", Path.GetFullPath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location) ?? ".")}
    }
};
foreach (var i in args)
{
    startInfo.ArgumentList.Add(i);
}
Process.Start(startInfo);

return 0;

void ShowError(string message)
{
#if Platforms_Windows
    PInvoke.MessageBox(HWND.Null, message,
        "ClassIsland", MESSAGEBOX_STYLE.MB_APPLMODAL | MESSAGEBOX_STYLE.MB_ICONSTOP);
#else
    Console.Error.WriteLine(message);
#endif
}