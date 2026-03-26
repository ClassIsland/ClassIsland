using System.Diagnostics;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Helpers;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Platforms.Linux.Services;

public class DesktopService : IDesktopService
{
    private static string StartupPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config/autostart/cn.classisland.app.desktop");

    private const string DesktopFileName = "cn.classisland.app.uri.desktop";
    private static string UserApplicationsDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/applications");
    private static string UserDesktopFile => Path.Combine(UserApplicationsDir, DesktopFileName);
    private static string SystemDesktopFile => Path.Combine("/usr/share/applications", DesktopFileName);
    private static string MimeType => $"x-scheme-handler/{IUriNavigationService.UriScheme}";

    public bool IsAutoStartEnabled
    {
        get =>
            File.Exists(StartupPath);
        set
        {
            try
            {
                var startupPath = StartupPath;
                if (!Path.Exists(Path.GetDirectoryName(startupPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(startupPath) ?? "");
                }
                if (value)
                {
                    _ = ShortcutHelpers.CreateFreedesktopShortcutAsync(startupPath);
                } else if (File.Exists(startupPath))
                {
                    File.Delete(startupPath);
                }
            }
            catch (Exception e)
            {
                IAppHost.TryGetService<ILogger<DesktopService>>()?.LogError(e, "无法设置自启动项目：{} ({})", StartupPath, value);
            }
        }
    }

    public bool IsUrlSchemeRegistered
    {
        get => IsUrlSchemeRegisteredInternal();
        set => SetUrlSchemeRegistered(value);
    }

    private static bool IsUrlSchemeRegisteredInternal()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        var defaultDesktop = TryGetDefaultDesktopFileFromXdgMime();
        if (!string.IsNullOrWhiteSpace(defaultDesktop) &&
            (string.Equals(defaultDesktop, DesktopFileName, StringComparison.OrdinalIgnoreCase)
             || string.Equals(defaultDesktop, UserDesktopFile, StringComparison.OrdinalIgnoreCase)
             || string.Equals(defaultDesktop, SystemDesktopFile, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        foreach (var file in new[] { UserDesktopFile, SystemDesktopFile })
        {
            if (!File.Exists(file))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            if (content.Contains(MimeType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void SetUrlSchemeRegistered(bool value)
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        try
        {
            if (value)
            {
                EnsureDesktopEntryExists();
                TryRegisterMimeHandler();
            }
            else
            {
                TryUnregisterMimeHandler();
                TryRemoveDesktopEntry();
            }
        }
        catch
        {
            // Best-effort only
        }
    }

    private static void EnsureDesktopEntryExists()
    {
        if (!Directory.Exists(UserApplicationsDir))
        {
            Directory.CreateDirectory(UserApplicationsDir);
        }

        if (File.Exists(UserDesktopFile))
        {
            return;
        }

        _ = ShortcutHelpers.CreateUriHandlerDesktopShortcutAsync(UserDesktopFile);
    }

    private static void TryRegisterMimeHandler()
    {
        TryRunProcess("xdg-mime", $"default {DesktopFileName} {MimeType}", out _);
    }

    private static void TryUnregisterMimeHandler()
    {
        RemoveMimeAppsEntry();
    }

    private static void TryRemoveDesktopEntry()
    {
        try
        {
            if (File.Exists(UserDesktopFile))
            {
                File.Delete(UserDesktopFile);
            }
        }
        catch
        {
            // ignore
        }
    }

    private static string? TryGetDefaultDesktopFileFromXdgMime()
    {
        if (!TryRunProcess("xdg-mime", $"query default {MimeType}", out var output))
        {
            return null;
        }

        return output?.Trim();
    }

    private static bool TryRunProcess(string fileName, string args, out string? output)
    {
        output = null;
        try
        {
            var psi = new ProcessStartInfo(fileName, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi);
            if (proc == null)
            {
                return false;
            }
            output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(5000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void RemoveMimeAppsEntry()
    {
        var paths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config/mimeapps.list"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/applications/mimeapps.list")
        };

        foreach (var path in paths)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var lines = File.ReadAllLines(path).ToList();
                var changed = false;
                for (var i = lines.Count - 1; i >= 0; i--)
                {
                    var line = lines[i].Trim();
                    if (line.StartsWith($"{MimeType}=", StringComparison.OrdinalIgnoreCase))
                    {
                        lines.RemoveAt(i);
                        changed = true;
                    }
                }

                if (changed)
                {
                    File.WriteAllLines(path, lines);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
