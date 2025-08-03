using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
#if IsMsix
using Windows.Storage;
#endif
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers.Native;
using ClassIsland.Core.Models;
using ClassIsland.Core.Models.Updating;
using ClassIsland.Helpers;
using ClassIsland.Models;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Helpers;
using ClassIsland.Views;
using Downloader;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry;
using DownloadProgressChangedEventArgs = Downloader.DownloadProgressChangedEventArgs;
using File = System.IO.File;

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
    private VersionInfo _selectedVersionInfo = new();

    public string CurrentUpdateSourceUrl => Settings.SelectedChannel;

    internal static string UpdateCachePath { get; } = Path.Combine(CommonDirectories.AppCacheFolderPath, "Update");

    internal const string UpdateMetadataUrl =
        "https://get.classisland.tech/d/ClassIsland-Ningbo-S3/classisland/disturb/index.json";

    public static string UpdateTempPath =>
#if IsMsix
        Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "UpdateTemp");
#else
        Path.Combine(CommonDirectories.AppRootFolderPath, "UpdateTemp");
#endif

    public VersionsIndex Index { get; set; }

    public VersionInfo SelectedVersionInfo
    {
        get => _selectedVersionInfo;
        set => SetField(ref _selectedVersionInfo, value);
    }

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

    private string MetadataPublisherPublicKey { get; }

    public event EventHandler? UpdateInfoUpdated;

    public UpdateService(SettingsService settingsService, ITaskBarIconService taskBarIconService, IHostApplicationLifetime lifetime,
        ISplashService splashService, ILogger<UpdateService> logger)
    {
        SettingsService = settingsService;
        TaskBarIconService = taskBarIconService;
        SplashService = splashService;
        Logger = logger;

        var keyStream = AssetLoader.Open(new Uri("avares://ClassIsland/Assets/TrustedPublicKeys/ClassIsland.MetadataPublisher.asc", UriKind.RelativeOrAbsolute));
        MetadataPublisherPublicKey = new StreamReader(keyStream).ReadToEnd();

        Index = ConfigureFileHelper.LoadConfig<VersionsIndex>(Path.Combine(UpdateCachePath, "Index.json"));
        SelectedVersionInfo = ConfigureFileHelper.LoadConfig<VersionInfo>(Path.Combine(UpdateCachePath, "SelectedVersionInfo.json"));


        SyncSpeedTestResults();
    }

    private void SyncSpeedTestResults()
    {
        foreach (var i in Index.Mirrors)
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
            && File.Exists(Path.Combine(UpdateTempPath, "update.zip")))
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


    public static async Task ReplaceApplicationFile(string target)
    {
        // TODO: 实现新版应用更新流程
        // var progressWindow = new UpdateProgressWindow();
        // progressWindow.Show();
        // if (!File.Exists(target))
        // {
        //     return;
        // }
        // var s = Environment.ProcessPath!;
        // var t = target;
        // Console.WriteLine(Path.GetFullPath(t));
        // Console.WriteLine(Path.GetDirectoryName(Path.GetFullPath(t)));
        // progressWindow.ProgressText = "正在备份应用数据……";
        // await FileFolderService.CreateBackupAsync(filename: $"Update_Backup_{App.AppVersion}_{DateTime.Now:yy-MMM-dd_HH-mm-ss}", rootPath: Path.GetDirectoryName(Path.GetFullPath(t)) ?? ".");
        // progressWindow.ProgressText = "正在等待应用退出……";
        // await Task.Run(() => NativeWindowHelper.WaitForFile(t));
        // progressWindow.ProgressText = "正在覆盖应用文件……";
        // await Task.Run(() => File.Copy(s, t, true));
        // progressWindow.CanClose = true;
        // progressWindow.Close();
    }

    public static void RemoveUpdateTemporary(string target)
    {
        if (File.Exists(target))
        {
            NativeWindowHelper.WaitForFile(target);
            File.Delete(target);
        }
        try
        {
            Directory.Delete(UpdateTempPath, true);
        }
        catch (Exception e)
        {
            // ignored
            Console.WriteLine(e);
        }
    }


    public async Task CheckUpdateAsync(bool isForce=false, bool isCancel=false)
    {
        var transaction = SentrySdk.StartTransaction("Get Update Info", "appUpdating.getMetadata");
        try
        {
            var spanGetIndex = transaction.StartChild("getIndex");
            CurrentWorkingStatus = UpdateWorkingStatus.CheckingUpdates;
            Index = await WebRequestHelper.SaveJson<VersionsIndex>(new Uri(UpdateMetadataUrl + $"?time={DateTime.Now.ToFileTimeUtc()}"), Path.Combine(UpdateCachePath, "Index.json"), verifySign:true, publicKey:MetadataPublisherPublicKey);
            SyncSpeedTestResults();
            var version = Index.Versions
                .Where(x => Version.TryParse(x.Version, out _) && x.Channels.Contains(Settings.SelectedUpdateChannelV2))
                .OrderByDescending(x => Version.Parse(x.Version))
                .FirstOrDefault();
            spanGetIndex.Finish(SpanStatus.Ok);
            if (version == null || !IsNewerVersion(isForce, isCancel, Version.Parse(version.Version)))
            {
                Settings.LastUpdateStatus = UpdateStatus.UpToDate;
                transaction.Finish(SpanStatus.Ok);
                return;
            }

            var spanGetDetail = transaction.StartChild("getDetail");
            SelectedVersionInfo = await WebRequestHelper.SaveJson<VersionInfo>(new Uri(version.VersionInfoUrl + $"?time={DateTime.Now.ToFileTimeUtc()}"), Path.Combine(UpdateCachePath, "SelectedVersionInfo.json"), verifySign: true, publicKey: MetadataPublisherPublicKey);
            Settings.LastUpdateStatus = UpdateStatus.UpdateAvailable;
            await PlatformServices.DesktopToastService.ShowToastAsync("发现新版本",
                $"{Assembly.GetExecutingAssembly().GetName().Version} -> {version.Version}\n" +
                "点击以查看详细信息。", UpdateNotificationClickedCallback);

            Settings.LastUpdateStatus = UpdateStatus.UpdateAvailable;
            spanGetDetail.Finish(SpanStatus.Ok);
            transaction.Finish(SpanStatus.Ok);
        }
        catch (Exception ex)
        {
            Settings.LastUpdateStatus = UpdateStatus.UpToDate;
            NetworkErrorException = ex;
            transaction.GetLastActiveSpan()?.Finish(ex, SpanStatus.InternalError);
            transaction.Finish(ex, SpanStatus.InternalError);
            Logger.LogError(ex, "检查应用更新失败。");
        }
        finally
        {
            Settings.LastCheckUpdateTime = DateTime.Now;
            UpdateInfoUpdated?.Invoke(this, EventArgs.Empty);
            CurrentWorkingStatus = UpdateWorkingStatus.Idle;
        }
    }

    private void UpdateNotificationClickedCallback()
    {
        IAppHost.GetService<IUriNavigationService>().NavigateWrapped(new Uri("classisland://app/settings/update"));
    }

    private bool IsNewerVersion(bool isForce, bool isCancel, Version verCode)
    {
        return (verCode > Assembly.GetExecutingAssembly().GetName().Version &&
                (Settings.LastUpdateStatus != UpdateStatus.UpdateDownloaded || isCancel)) // 正常更新
               || isForce;
    }

    public async Task DownloadUpdateAsync()
    {
        if (Design.IsDesignMode || true)
        {
            return;
        }
        var transaction = SentrySdk.StartTransaction("Download Update", "appUpdating.download");
        var spanDeletePreviousFile = transaction.StartChild("deletePreviousFile");
        try
        {
            if (Directory.Exists(UpdateTempPath))
            {
                Directory.Delete(UpdateTempPath, true);
            }
            spanDeletePreviousFile.Finish(SpanStatus.Ok);
        }
        catch (Exception ex)
        {
            spanDeletePreviousFile.Finish(ex, SpanStatus.InternalError);
            Logger.LogError(ex, "移除下载临时文件失败。");
        }

        var spanDownload = transaction.StartChild("download");
        try
        {
            var downloadInfo = SelectedVersionInfo.DownloadInfos[AppBase.Current.AppSubChannel];
            Settings.UpdateArtifactHash = downloadInfo.ArchiveSHA256;
            Logger.LogInformation("下载应用更新包：{}", downloadInfo.ArchiveDownloadUrls[Settings.SelectedUpdateMirrorV2]);
            TotalSize = 0;
            DownloadedSize = 0;
            DownloadSpeed = 0;
            DownloadStatusUpdateStopwatch.Start();
            CurrentWorkingStatus = UpdateWorkingStatus.DownloadingUpdates;
            transaction.SetExtra("download.url", downloadInfo.ArchiveDownloadUrls[Settings.SelectedUpdateMirrorV2]);

            Downloader = DownloadBuilder.New()
                .WithUrl(downloadInfo.ArchiveDownloadUrls[Settings.SelectedUpdateMirrorV2])
                .Configure((c) =>
                {
                    c.ChunkCount = 32;
                    c.ParallelCount = 32;
                    c.ParallelDownload = true;
                    //c.Timeout = 4096;
                })
                .WithDirectory(UpdateTempPath)
                .WithFileName("update.zip")
                .Build();
            Downloader.DownloadProgressChanged += DownloaderOnDownloadProgressChanged;
            Downloader.DownloadFileCompleted += (sender, args) =>
            {
                DownloadStatusUpdateStopwatch.Stop();
                transaction.SetExtra("download.size", Downloader.TotalFileSize);
                var speed = DownloadStatusUpdateStopwatch.Elapsed.TotalSeconds == 0
                    ? 0.0
                    : Downloader.TotalFileSize / DownloadStatusUpdateStopwatch.Elapsed.TotalSeconds;
                transaction.SetExtra("download.bytesPerSecond", speed);
                DownloadStatusUpdateStopwatch.Reset();
                if (IsCanceled)
                {
                    IsCanceled = false;
                    spanDownload.Finish(SpanStatus.Cancelled);
                    transaction.Finish(SpanStatus.Cancelled);
                    return;
                }

                if (!File.Exists(Path.Combine(UpdateTempPath, @"update.zip")) || args.Error != null)
                {
                    //await RemoveDownloadedFiles();
                    if (args.Error != null)
                    {
                        spanDownload.Finish(args.Error, SpanStatus.InternalError);
                    }
                    else
                    {
                        spanDownload.Finish(SpanStatus.InternalError);
                    }
                    throw new Exception("更新下载失败。", args.Error);
                }
                else
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        Settings.LastUpdateStatus = UpdateStatus.UpdateDownloaded;
                    });
                    spanDownload.Finish(SpanStatus.Ok);
                    transaction.Finish(SpanStatus.Ok);
                }
            };
            await Downloader.StartAsync();
        }
        catch (Exception ex)
        {
            NetworkErrorException = ex;
            transaction.Finish(ex, SpanStatus.InternalError);
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
            Directory.Delete(UpdateTempPath, true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "移除下载临时文件失败。");
        }
        await CheckUpdateAsync(isCancel:true);
    }

    private void DownloaderOnDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        if (DownloadStatusUpdateStopwatch.ElapsedMilliseconds < 250)
            return;
        DownloadStatusUpdateStopwatch.Restart();
        TotalSize = e.TotalBytesToReceive;
        DownloadedSize = e.ReceivedBytesSize;
        DownloadSpeed = e.BytesPerSecondSpeed;
        
        DownloadEtcSeconds = TimeSpanHelper.FromSecondsSafe(DownloadSpeed == 0 ? 0 : (long)((TotalSize - DownloadedSize) / DownloadSpeed));
        Logger.LogInformation("Download progress changed: {}/{} ({}B/s)", TotalSize, DownloadedSize, DownloadSpeed);
    }

    public async Task ExtractUpdateAsync()
    {
        Logger.LogInformation("正在展开应用更新包。");
        await Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(Path.Combine(UpdateTempPath, @"./update.zip"), Path.Combine(UpdateTempPath, @"./extracted"), true);
        });
    }

    private async Task ValidateUpdateAsync()
    {
        if (string.IsNullOrWhiteSpace(Settings.UpdateArtifactHash))
        {
            Logger.LogWarning("未找到缓存的校验信息，跳过更新文件校验。");
            return;
        }

        await using var stream = File.OpenRead(Path.Combine(UpdateTempPath, @"./update.zip"));
        var sha256 = await SHA256.HashDataAsync(stream);
        var str = BitConverter.ToString(sha256);
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
        var transaction = SentrySdk.StartTransaction("Reboot to Update Mode", "appUpdating.rebootToUpdate");
        Logger.LogInformation("正在重启至升级模式。");
        await PlatformServices.DesktopToastService.ShowToastAsync("正在安装应用更新", "这可能需要10-30秒的时间，请稍后……");
        CurrentWorkingStatus = UpdateWorkingStatus.ExtractingUpdates;
        try
        {
            var spanValidate = transaction.StartChild("validate");
            await ValidateUpdateAsync();
            spanValidate.Finish(SpanStatus.Ok);

            var spanExtract = transaction.StartChild("extract");
            await ExtractUpdateAsync();
            spanExtract.Finish(SpanStatus.Ok);

            var spanReboot = transaction.StartChild("reboot");
            Process.Start(new ProcessStartInfo()
            {
                FileName = Path.Combine(UpdateTempPath, @"extracted/ClassIsland.exe"),
                ArgumentList =
                {
                    "-urt", Environment.ProcessPath!,
                    "-m", "true"
                }
            });
            AppBase.Current.Stop();
            spanReboot.Finish(SpanStatus.Ok);
            transaction.Finish(SpanStatus.Ok);
        }
        catch (Exception ex)
        {
            success = false;
            transaction.GetLastActiveSpan()?.Finish(ex, SpanStatus.InternalError);
            transaction.Finish(ex, SpanStatus.InternalError);
            Logger.LogError(ex, "无法安装更新");
            await PlatformServices.DesktopToastService.ShowToastAsync("安装更新失败", ex.Message, UpdateNotificationClickedCallback);
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