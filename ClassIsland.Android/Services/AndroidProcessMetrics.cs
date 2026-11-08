using System.Globalization;
using Android.OS;
using AndroidProcess = global::Android.OS.Process;

namespace ClassIsland.Android.Services;

internal sealed class AndroidProcessMetrics
{
    private long? _previousCpuTime;
    private long? _previousElapsedTime;

    internal string Read()
    {
        var cpu = ReadCpu();
        var rss = ReadMemory(ReadResidentMemory);
        var managed = ReadMemory(() => GC.GetTotalMemory(false));
        var committed = ReadMemory(() => GC.GetGCMemoryInfo().TotalCommittedBytes);
        var native = ReadMemory(() => global::Android.OS.Debug.NativeHeapAllocatedSize);
        return $"进程 RSS：{rss}\n.NET 托管堆（估算）：{managed}\nGC 已提交：{committed}\n原生堆已分配：{native}\nCPU：{cpu}";
    }

    private string ReadCpu()
    {
        try
        {
            var elapsedTime = SystemClock.ElapsedRealtime();
            var cpuTime = AndroidProcess.ElapsedCpuTime;
            var usage = _previousCpuTime is { } previousCpu &&
                        _previousElapsedTime is { } previousElapsed && elapsedTime > previousElapsed
                ? (cpuTime - previousCpu) * 100.0 / (elapsedTime - previousElapsed)
                : (double?)null;
            _previousCpuTime = cpuTime;
            _previousElapsedTime = elapsedTime;
            return usage is >= 0 ? $"{usage.Value:F1}%" : "采样中…";
        }
        catch (Exception)
        {
            _previousCpuTime = null;
            _previousElapsedTime = null;
            return "暂不可用";
        }
    }

    private static string ReadMemory(Func<long> read)
    {
        try
        {
            return $"{read() / 1048576.0:F1} MiB";
        }
        catch (Exception)
        {
            return "暂不可用";
        }
    }

    private static long ReadResidentMemory()
    {
        foreach (var line in File.ReadLines("/proc/self/status"))
        {
            if (!line.StartsWith("VmRSS:", StringComparison.Ordinal))
            {
                continue;
            }

            var fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            return checked(long.Parse(fields[1], CultureInfo.InvariantCulture) * 1024);
        }

        throw new InvalidDataException("进程内存统计不可用。");
    }
}
