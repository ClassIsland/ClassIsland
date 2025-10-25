namespace ClassIsland.Platforms.MacOs.Models;

public class WindowInfo
{
    public long WinId { get; }
    public string ProcessName { get; }
    public int Pid { get; }
    public string Title { get; }
    public CGRect Bounds { get; }
    public int WindowLevel { get; }

    internal WindowInfo(long winId, string processName, int pid, string title, CGRect bounds, int windowLevel)
    {
        WinId = winId;
        ProcessName = processName;
        Pid = pid;
        Title = title;
        Bounds = bounds;
        WindowLevel = windowLevel;
    }
}