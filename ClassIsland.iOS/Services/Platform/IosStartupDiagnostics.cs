namespace ClassIsland.iOS.Services.Platform;

internal static class IosStartupDiagnostics
{
    private static readonly object SyncRoot = new();
    private static string? _logPath;

    internal static void Initialize(string documentsDirectory)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Write("Unhandled managed exception", args.ExceptionObject as Exception);

        try
        {
            // 不依赖尚未初始化的应用路径和日志服务，保留上一次启动的记录供排障。
            var directory = Path.Combine(documentsDirectory, "ClassIsland", "Diagnostics");
            Directory.CreateDirectory(directory);
            var logPath = Path.Combine(directory, "startup-latest.log");
            if (File.Exists(logPath))
            {
                File.Move(logPath, Path.Combine(directory, "startup-previous.log"), true);
            }

            _logPath = logPath;
            Write("Managed entry point reached");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Unable to initialize startup diagnostics: {exception}");
        }
    }

    internal static void Write(string stage, Exception? exception = null)
    {
        try
        {
            lock (SyncRoot)
            {
                if (_logPath == null)
                {
                    return;
                }

                // 每次关闭文件，避免进程在异常后终止时丢失缓冲中的内容。
                File.AppendAllText(_logPath,
                    $"[{DateTimeOffset.Now:O}] {stage}{Environment.NewLine}" +
                    (exception == null ? "" : $"{exception}{Environment.NewLine}"));
            }
        }
        catch (Exception writeException)
        {
            Console.Error.WriteLine($"Unable to write startup diagnostics: {writeException}");
        }
    }
}
