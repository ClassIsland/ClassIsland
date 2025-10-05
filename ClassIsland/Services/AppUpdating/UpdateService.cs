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
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
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
using PhainonDistributionCenter.Shared.Models.Api.Responses.Distribution;
using PhainonDistributionCenter.Shared.Models.Client;
using PhainonDistributionCenter.Shared.Models.FileMap;
using Sentry;
using Tmds.DBus.Protocol;
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
    private DistributionInfoClient _distributionInfo;
    private string _currentWorkingMessage = "";

    public string CurrentUpdateSourceUrl => Settings.SelectedChannel;

    internal static string UpdateCachePath { get; } = Path.Combine(CommonDirectories.AppCacheFolderPath, "Update");

    public static string UpdateTempPath => Path.Combine(CommonDirectories.AppTempFolderPath, "Updating");

    private static string UpdateDistributionInfoPath = Path.Combine(UpdateCachePath, "DistributionInfo.json");
    private static string UpdateDistributionMetadataPath = Path.Combine(UpdateCachePath, "DistributionMetadata.json");

    public DistributionInfoClient DistributionInfo
    {
        get => _distributionInfo;
        set => SetField(ref _distributionInfo, value);
    }

    public DistributionMetadata DistributionMetadata
    {
        get;
        set;
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

    public string CurrentWorkingMessage
    {
        get => _currentWorkingMessage;
        set => SetField(ref _currentWorkingMessage, value);
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
    
    private WebRequestHelper RequestHelper { get; }

    public UpdateService(SettingsService settingsService, ITaskBarIconService taskBarIconService, IHostApplicationLifetime lifetime,
        ISplashService splashService, ILogger<UpdateService> logger)
    {
        SettingsService = settingsService;
        TaskBarIconService = taskBarIconService;
        SplashService = splashService;
        Logger = logger;

        var keyStream = AssetLoader.Open(new Uri("avares://ClassIsland/Assets/TrustedPublicKeys/ClassIsland.MetadataPublisher.asc", UriKind.RelativeOrAbsolute));
        MetadataPublisherPublicKey = new StreamReader(keyStream).ReadToEnd();

        RequestHelper = new WebRequestHelper(new Uri("http://localhost:5205"), true);
        _distributionInfo = ConfigureFileHelper.LoadConfig<DistributionInfoClient>(UpdateDistributionInfoPath);
        DistributionMetadata = ConfigureFileHelper.LoadConfig<DistributionMetadata>(UpdateDistributionMetadataPath);
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
            var subChannel =
                AppBase.Current.IsDevelopmentBuild && !string.IsNullOrWhiteSpace(Settings.DebugSubChannelOverride)
                    ? Settings.DebugSubChannelOverride
                    : AppBase.Current.AppSubChannel;
            CurrentWorkingStatus = UpdateWorkingStatus.CheckingUpdates;
            DistributionMetadata = await RequestHelper.SaveJson<DistributionMetadata>(
                new Uri("api/v1/public/distributions/metadata", UriKind.Relative), UpdateDistributionMetadataPath);
            var latest = await RequestHelper.GetJson<LatestDistributionInfoMinResponse>(
                new Uri($"api/v1/public/distributions/latest/{Settings.SelectedUpdateChannelV3}", UriKind.Relative));
            spanGetIndex.Finish(SpanStatus.Ok);
            if (!IsNewerVersion(isForce, isCancel, Version.Parse(latest.Version)))
            {
                Settings.LastUpdateStatus = UpdateStatus.UpToDate;
                transaction.Finish(SpanStatus.Ok);
                return;
            }

            var spanGetDetail = transaction.StartChild("getDetail");
            DistributionInfo = await RequestHelper.SaveJson<DistributionInfoClient>(
                new Uri($"api/v1/public/distributions/{latest.DistributionId}/{subChannel}", UriKind.Relative),
                UpdateDistributionInfoPath);
            Settings.LastUpdateStatus = UpdateStatus.UpdateAvailable;
            await PlatformServices.DesktopToastService.ShowToastAsync("发现新版本",
                $"{Assembly.GetExecutingAssembly().GetName().Version} -> {latest.Version}\n" +
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
        return;
        if (Design.IsDesignMode)
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
            var valid = DetachedSignatureProcessor.VerifyDetachedSignature(DistributionInfo.FileMapJson,
                Encoding.UTF8.GetBytes(DistributionInfo.FileMapSignature), MetadataPublisherPublicKey);
            if (!valid)
            {
                throw new InvalidOperationException("文件图签名校验不通过");
            }

            var fileMap = JsonSerializer.Deserialize<FileMap>(DistributionInfo.FileMapJson);
            
            TotalSize = 0;
            DownloadedSize = 0;
            DownloadSpeed = 0;
            DownloadStatusUpdateStopwatch.Start();
            CurrentWorkingStatus = UpdateWorkingStatus.DownloadingUpdates;

            var options = new DownloadConfiguration()
            {
                ChunkCount = 32,
                ParallelCount = 8,
                ParallelDownload = true
            };
            
            
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