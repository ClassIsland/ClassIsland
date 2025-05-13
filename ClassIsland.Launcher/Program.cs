using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

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
    .FirstOrDefault();

if (installation == null || !File.Exists(Path.Combine(installation, "ClassIsland.exe")))
{
    PInvoke.MessageBox(HWND.Null, "找不到有效的 ClassIsland 版本，可能是安装已损坏。请在 https://classisland.tech/download 重新下载并安装 ClassIsland。",
        "ClassIsland", MESSAGEBOX_STYLE.MB_APPLMODAL | MESSAGEBOX_STYLE.MB_ICONSTOP);
    return 1;
}

var startInfo = new ProcessStartInfo()
{
    FileName = Path.Combine(Path.Combine(installation, "ClassIsland.exe")),
    WorkingDirectory = root
};
foreach (var i in args)
{
    startInfo.ArgumentList.Add(i);
}
Process.Start(startInfo);

return 0;
