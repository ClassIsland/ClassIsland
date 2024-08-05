using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Helpers.Native;
using ClassIsland.Helpers;
using ClassIsland.Models;
using ClassIsland.Shared.Enums;
using Downloader;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Octokit;
using Application = System.Windows.Application;
using DownloadProgressChangedEventArgs = Downloader.DownloadProgressChangedEventArgs;
using File = System.IO.File;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace ClassIsland.Services.AppUpdating;

public class UpdateService : IHostedService, INotifyPropertyChanged
{
    private UpdateWorkingStatus _currentWorkingStatus = UpdateWorkingStatus.Idle;
    private long _downloadedSize = 0;
    private long _totalSize = 0;
    private double _downloadSpeed = 0;
    public IDownload? Downloader;
    private bool _isCanceled = false;
    private Exception? _networkErrorException;
    private TimeSpan _downloadEtcSeconds = TimeSpan.Zero;

    public string CurrentUpdateSourceUrl => Settings.SelectedChannel;

    public static string AppCenterSourceKey { get; } = "8593a4a2-0848-40ca-a87c-16e46bf5f695";
    public static string GitHubSourceKey { get; } = "05cb3142-d4ea-4eb0-9dfc-ddc3af6e20b0";
    public static string GhProxySourceKey { get; } = "454a648f-12a0-485e-ae85-10a738e25679";

    public static Dictionary<string, UpdateSource> UpdateSources = new()
    {
        {AppCenterSourceKey, new UpdateSource()
        {
            Name = "Microsoft App Center",
            Kind = UpdateSourceKind.AppCenter,
            SpeedTestSources =
            {
                "install.appcenter.ms",
                "appcenter-filemanagement-distrib1ede6f06e.azureedge.net"
            }
        }},
        {GitHubSourceKey, new UpdateSource()
        {
            Name = "GitHub",
            Kind = UpdateSourceKind.GitHub,
            SpeedTestSources =
            {
                "api.github.com",
                "objects.githubusercontent.com"
            }
        }},
        {GhProxySourceKey, new UpdateSource()
        {
            Name = "GitHub（ghproxy镜像）",
            Kind = UpdateSourceKind.GitHub,
            SpeedTestSources =
            {
                "api.github.com",
                "mirror.ghproxy.com"
            }
        }}
    };

    public Stopwatch DownloadStatusUpdateStopwatch { get; } = new();

    public UpdateWorkingStatus CurrentWorkingStatus
    {
        get => _currentWorkingStatus;
        set => SetField(ref _currentWorkingStatus, value);
    }

    private SettingsService SettingsService
    {
        get;
    }

    private Settings Settings => SettingsService.Settings;

    private ITaskBarIconService TaskBarIconService
    {
        get;
    }

    private ISplashService SplashService { get; }

    private ILogger<UpdateService> Logger { get; }

    public UpdateService(SettingsService settingsService, ITaskBarIconService taskBarIconService, IHostApplicationLifetime lifetime,
        ISplashService splashService, ILogger<UpdateService> logger)
    {
        SettingsService = settingsService;
        TaskBarIconService = taskBarIconService;
        SplashService = splashService;
        Logger = logger;

        if (AppBase.Current.IsAssetsTrimmed())
        {
            foreach (var i in UpdateSources.Where(x => x.Value.Kind == UpdateSourceKind.AppCenter).ToList())
            {
                UpdateSources.Remove(i.Key);
            }
        }

        if (!UpdateSources.ContainsKey(Settings.SelectedUpgradeMirror))
        {
            Settings.SelectedUpgradeMirror = UpdateSources.Keys.FirstOrDefault() ?? Settings.SelectedUpgradeMirror;
        }

        foreach (var i in UpdateSources)
        {
            if (!Settings.SpeedTestResults.ContainsKey(i.Key))
            {
                Settings.SpeedTestResults.Add(i.Key, new SpeedTestResult());
            }
            i.Value.SpeedTestResult = Settings.SpeedTestResults[i.Key];
        }
    }

    public bool IsCanceled
    {
        get => _isCanceled;
        set => SetField(ref _isCanceled, value);
    }

    public long DownloadedSize
    {
        get => _downloadedSize;
        set => SetField(ref _downloadedSize, value);
    }

    public long TotalSize
    {
        get => _totalSize;
        set => SetField(ref _totalSize, value);
    }

    public double DownloadSpeed
    {
        get => _downloadSpeed;
        set => SetField(ref _downloadSpeed, value);
    }

    public Exception? NetworkErrorException
    {
        get => _networkErrorException;
        set => SetField(ref _networkErrorException, value);
    }

    public TimeSpan DownloadEtcSeconds
    {
        get => _downloadEtcSeconds;
        set => SetField(ref _downloadEtcSeconds, value);
    }

    public async Task<bool> AppStartup()
    {
        if (Settings.AutoInstallUpdateNextStartup 
            && Settings.LastUpdateStatus == UpdateStatus.UpdateDownloaded
            && File.Exists(".\\UpdateTemp\\update.zip"))
        {
            SplashService.SplashStatus = "正在准备更新…";
            App.GetService<ISplashService>().CurrentProgress = 90;
            return await RestartAppToUpdateAsync();
        }

        if (Settings.UpdateMode < 1)
        {
            return false;
        }
        
        _ = AppStartupBackground();
        return false;
    }

    private async Task AppStartupBackground()
    {
        if (Settings.IsAutoSelectUpgradeMirror && DateTime.Now - Settings.LastSpeedTest >= TimeSpan.FromDays(7))
        {
            await App.GetService<UpdateNodeSpeedTestingService>().RunSpeedTestAsync();

        }
        await CheckUpdateAsync();

        if (Settings.UpdateMode < 2)
        {
            return;
        }

        if (Settings.LastUpdateStatus == UpdateStatus.UpdateAvailable)
        {
            await DownloadUpdateAsync();
            Settings.AutoInstallUpdateNextStartup = false;
        }

        if (Settings.UpdateMode < 3)
        {
            return;
        }

        if (Settings.LastUpdateStatus == UpdateStatus.UpdateDownloaded)
        {
            Settings.AutoInstallUpdateNextStartup = true;
        }
    }

    public static string AppCenterBetaRootUrl { get; } =
        "https://install.appcenter.ms/api/v0.1/apps/hellowrc/classisland/distribution_groups/publicbeta";

    public static readonly ObservableCollection<UpdateChannel> UpdateChannels = new()
    {
        new UpdateChannel
        {
            Name = "稳定通道",
            Description = "接收应用稳定版的更新，包含较新且稳定的特性和改进。",
            RootUrl = "https://install.appcenter.ms/api/v0.1/apps/hellowrc/classisland/distribution_groups/public",
            RootUrlGitHub = "https://api.github.com/repos/HelloWRC/ClassIsland/releases"
        },
        new UpdateChannel
        {
            Name = "测试通道",
            Description = "接收应用最新的测试版更新，包含最新的特性和改进，可能包含较多的缺陷和未完工的功能。",
            RootUrl = AppCenterBetaRootUrl,
            RootUrlGitHub = "https://api.github.com/repos/HelloWRC/ClassIsland/releases"
        }
    };

    public static void ReplaceApplicationFile(string target)
    {
        if (!File.Exists(target))
        {
            return;
        }
        var s = Environment.ProcessPath!;
        var t = target;
        NativeWindowHelper.WaitForFile(t);
        File.Move(s, t, true);
    }

    public static void RemoveUpdateTemporary(string target)
    {
        if (!File.Exists(target))
        {
            return;
        }
        NativeWindowHelper.WaitForFile(target);
        File.Delete(target);
        try
        {
            Directory.Delete("./UpdateTemp", true);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    
    public static async Task<List<AppCenterReleaseInfoMin>> GetUpdateVersionsAsync(string queryRoot)
    {
        return await WebRequestHelper.GetJson<List<AppCenterReleaseInfoMin>>(new Uri(queryRoot));
    }

    public static async Task<IReadOnlyList<Release>> GetUpdateVersionsGitHubAsync(string? key=null)
    {
        var github = new GitHubClient(new ProductHeaderValue("ClassIsland"));
        if (!string.IsNullOrEmpty(key))
        {
            github.Credentials = new Credentials(key);
        }
        var r = await github.Repository.Release.GetAll("HelloWRC", "ClassIsland");
        return r;
        //throw new ArgumentException("Releases info array is null!");
    }

    public static async Task<AppCenterReleaseInfo> GetVersionArtifactsAsync(string versionRoot)
    {
        return await WebRequestHelper.GetJson<AppCenterReleaseInfo>(new Uri(versionRoot));
    }

    

    public async Task CheckUpdateAsync(bool isForce=false, bool isCancel=false)
    {
        try
        {
            var kind = UpdateSources[Settings.SelectedUpgradeMirror].Kind;
            CurrentWorkingStatus = UpdateWorkingStatus.CheckingUpdates;
            Settings.UpdateArtifactHash = "";
            Version verCode;
            Logger.LogInformation("正在检查应用更新。{}", kind);

            if (kind == UpdateSourceKind.GitHub)
            {
                // 使用 GitHub 获取更新
                var versionsGh = await GetUpdateVersionsGitHubAsync(Settings.DebugGitHubAuthKey);
                if (versionsGh.Count <= 0)
                {
                    CurrentWorkingStatus = UpdateWorkingStatus.Idle;
                    return;
                }

                var v = (versionsGh.Where(i => (CurrentUpdateSourceUrl == AppCenterBetaRootUrl || !i.Prerelease) 
                                               && Version.TryParse((string?)i.TagName, out _))
                    .OrderByDescending(i => Version.Parse(i.TagName))).First();
                var fileName = AppBase.Current.IsAssetsTrimmed() ? "ClassIsland_AssetsTrimmed.zip" : "ClassIsland.zip";
                verCode = Version.Parse(v.TagName);
                Settings.UpdateReleaseInfo = v.Body;
                Settings.UpdateArtifactHash = ChecksumHelper.ExtractHashInfo(v.Body, fileName);
                Settings.LastCheckUpdateInfoCacheGitHub = v;
                var assetsUrl = v.Assets.First(x => x.Name == fileName).BrowserDownloadUrl;    
                Settings.UpdateDownloadUrl = Settings.SelectedUpgradeMirror == GhProxySourceKey ? $"https://mirror.ghproxy.com/{assetsUrl}" : assetsUrl;
            }
            else
            {
                var versions = await GetUpdateVersionsAsync(CurrentUpdateSourceUrl + "/public_releases");
                if (versions.Count <= 0)
                {
                    CurrentWorkingStatus = UpdateWorkingStatus.Idle;
                    return;
                }

                var v = versions.Where(i => Version.TryParse(i.Version, out _)).OrderByDescending(i => Version.Parse(i.Version)).First();
                verCode = Version.Parse(v.Version);
                if (IsNewerVersion(isForce, isCancel, verCode))
                {
                    Settings.LastCheckUpdateInfoCache =
                        await GetVersionArtifactsAsync(CurrentUpdateSourceUrl + $"/releases/{v.Id}");
                    Settings.UpdateReleaseInfo = Settings.LastCheckUpdateInfoCache.ReleaseNotes;
                    Settings.UpdateDownloadUrl = Settings.LastCheckUpdateInfoCache.DownloadUrl;
                    Settings.UpdateArtifactHash = Settings.LastCheckUpdateInfoCache.Fingerprint;
                }
            }

            Settings.LastUpdateSourceKind = kind;
            if (IsNewerVersion(isForce, isCancel, verCode)) 
            {
                Settings.LastUpdateStatus = UpdateStatus.UpdateAvailable;
                Settings.UpdateVersion = verCode;
                TaskBarIconService.MainTaskBarIcon.ShowNotification("发现新版本",
                    $"{Assembly.GetExecutingAssembly().GetName().Version} -> {verCode}\n" +
                    "点击以查看详细信息。");
                Logger.LogDebug("更新包MD5：{}", Settings.UpdateArtifactHash);
                if (Settings.UpdateArtifactHash == "")
                {
                    Logger.LogWarning("未获取到更新包MD5校验值。");
                }
            }
            else
            {
                Settings.LastUpdateStatus = UpdateStatus.UpToDate;
            }

            Settings.LastCheckUpdateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            Settings.LastUpdateStatus = UpdateStatus.UpToDate;
            NetworkErrorException = ex;
            Logger.LogError(ex, "检查应用更新失败。");
        }
        finally
        {
            CurrentWorkingStatus = UpdateWorkingStatus.Idle;
        }
    }

    private bool IsNewerVersion(bool isForce, bool isCancel, Version verCode)
    {
        return (verCode > Assembly.GetExecutingAssembly().GetName().Version &&
                (Settings.LastUpdateStatus != UpdateStatus.UpdateDownloaded || isCancel)) // 正常更新
               || isForce;
    }

    public async Task DownloadUpdateAsync()
    {
        try
        {
            if (Directory.Exists("./UpdateTemp"))
            {
                Directory.Delete("./UpdateTemp", true);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "移除下载临时文件失败。");
        }
        try
        {
            Logger.LogInformation("下载应用更新包：{}", Settings.UpdateDownloadUrl);
            TotalSize = 0;
            DownloadedSize = 0;
            DownloadSpeed = 0;
            DownloadStatusUpdateStopwatch.Start();
            CurrentWorkingStatus = UpdateWorkingStatus.DownloadingUpdates;

            Downloader = DownloadBuilder.New()
                .WithUrl(Settings.UpdateDownloadUrl)
                .Configure((c) =>
                {
                    c.ChunkCount = 32;
                    c.ParallelCount = 32;
                    c.ParallelDownload = true;
                    //c.Timeout = 4096;
                })
                .WithDirectory(@".\UpdateTemp")
                .WithFileName("update.zip")
                .Build();
            Downloader.DownloadProgressChanged += DownloaderOnDownloadProgressChanged;
            await Downloader.StartAsync();
            DownloadStatusUpdateStopwatch.Stop();
            DownloadStatusUpdateStopwatch.Reset();
            if (IsCanceled)
            {
                IsCanceled = false;
                return;
            }

            if (!File.Exists(@".\UpdateTemp\update.zip"))
            {
                //await RemoveDownloadedFiles();
                throw new Exception("更新下载失败。");
            }
            else
            {
                Settings.LastUpdateStatus = UpdateStatus.UpdateDownloaded;
            }

        }
        catch (Exception ex)
        {
            NetworkErrorException = ex;
            Logger.LogError(ex, "下载应用更新失败。");
            await RemoveDownloadedFiles();
        }
        finally
        {
            CurrentWorkingStatus = UpdateWorkingStatus.Idle;
        }

    }

    public async void StopDownloading()
    {
        if (Downloader == null)
        {
            return;
        }

        Logger.LogInformation("应用更新下载停止。");
        IsCanceled = true;
        Downloader.Pause();
        Downloader.Dispose();
        CurrentWorkingStatus = UpdateWorkingStatus.Idle;
        await RemoveDownloadedFiles();
    }

    public async Task RemoveDownloadedFiles()
    {
        try
        {
            Directory.Delete("./UpdateTemp", true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "移除下载临时文件失败。");
        }
        await CheckUpdateAsync(isCancel:true);
    }

    private void DownloaderOnDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        if (DownloadStatusUpdateStopwatch.ElapsedMilliseconds < 1000)
            return;
        DownloadStatusUpdateStopwatch.Restart();
        TotalSize = e.TotalBytesToReceive;
        DownloadedSize = e.ReceivedBytesSize;
        DownloadSpeed = e.BytesPerSecondSpeed;
        DownloadEtcSeconds = TimeSpan.FromSeconds((long)((TotalSize - DownloadedSize) / DownloadSpeed));
        Logger.LogInformation("Download progress changed: {}/{} ({}B/s)", TotalSize, DownloadedSize, DownloadSpeed);

    }

    public async Task ExtractUpdateAsync()
    {
        Logger.LogInformation("正在展开应用更新包。");
        await Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(@"./UpdateTemp/update.zip", "./UpdateTemp/extracted", true);
        });
    }

    private async Task ValidateUpdateAsync()
    {
        if (string.IsNullOrWhiteSpace(Settings.UpdateArtifactHash))
        {
            Logger.LogWarning("未找到缓存的校验信息，跳过更新文件校验。");
            return;
        }

        await using var stream = File.OpenRead(@"./UpdateTemp/update.zip");
        var md5 = await MD5.HashDataAsync(stream);
        var str = BitConverter.ToString(md5);
        str = str.Replace("-", "");
        Logger.LogDebug("更新文件哈希：{}", str);
        if (!string.Equals(str, Settings.UpdateArtifactHash, StringComparison.CurrentCultureIgnoreCase))
        {
            throw new Exception("更新文件校验失败，可能下载已经损坏。");
        }
    }

    public async Task<bool> RestartAppToUpdateAsync()
    {
        var success = true;
        Logger.LogInformation("正在重启至升级模式。");
        TaskBarIconService.MainTaskBarIcon.ShowNotification("正在安装应用更新", "这可能需要10-30秒的时间，请稍后……");
        CurrentWorkingStatus = UpdateWorkingStatus.ExtractingUpdates;
        try
        {
            await ValidateUpdateAsync();
            await ExtractUpdateAsync();
            Process.Start(new ProcessStartInfo()
            {
                FileName = "./UpdateTemp/extracted/ClassIsland.exe",
                ArgumentList =
                {
                    "-urt", Environment.ProcessPath!,
                    "-m", "true"
                }
            });
            AppBase.Current.Stop();
        }
        catch (Exception ex)
        {
            success = false;
            Logger.LogError(ex, "无法安装更新");
            TaskBarIconService.MainTaskBarIcon.ShowNotification("安装更新失败", ex.Message);
            CurrentWorkingStatus = UpdateWorkingStatus.Idle;
            await RemoveDownloadedFiles();
        }

        return success;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        return;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        return;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}