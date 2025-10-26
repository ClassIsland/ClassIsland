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
using ClassIsland.Enums.AppUpdating;
using ClassIsland.Helpers;
using ClassIsland.Models;
using ClassIsland.Models.AppUpdating;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Helpers;
using ClassIsland.Views;
using Downloader;
using DynamicData;
using ICSharpCode.SharpZipLib.GZip;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhainonDistributionCenter.Shared.Helpers;
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
    public IDownload? Downloader;
    private bool _isCanceled = false;
    private Exception? _networkErrorException;
    private TimeSpan _downloadEtcSeconds = TimeSpan.Zero;
    private DistributionInfoClient _distributionInfo;
    private string _currentWorkingMessage = "";

    private const string PhainonRootUrl = "https://distribution.classisland.tech";

    internal static string UpdateCachePath { get; } = Path.Combine(CommonDirectories.AppCacheFolderPath, "Update");

    public static string UpdateTempPath => Path.Combine(CommonDirectories.AppTempFolderPath, "Updating");

    private static string UpdateDistributionInfoPath { get; } = Path.Combine(UpdateCachePath, "DistributionInfo.json");
    private static string UpdateDistributionMetadataPath { get; } = Path.Combine(UpdateCachePath, "DistributionMetadata.json");
    
    private const string AppComponentName = "app";
    
    private const string LauncherComponentName = "launcher";

    private static readonly string[] Components = [AppComponentName, LauncherComponentName];
    public static readonly string[] AllowedPackageTypes = ["folder", "folderClassic", "installer"];

    private CancellationTokenSource? _downloadCancellationTokenSource;
    private int _downloadedCount = 0;
    private int _downloadingCountTotal = 0;

    public ObservableCollection<DownloadTaskInfo> DownloadTasks { get; } = [];

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

    public int DownloadingCountTotal
    {
        get => _downloadingCountTotal;
        set
        {
            if (value == _downloadingCountTotal) return;
            _downloadingCountTotal = value;
            OnPropertyChanged();
        }
    }

    public int DownloadedCount
    {
        get => _downloadedCount;
        set
        {
            if (value == _downloadedCount) return;
            _downloadedCount = value;
            OnPropertyChanged();
        }
    }

    public UpdateService(SettingsService settingsService, ITaskBarIconService taskBarIconService, IHostApplicationLifetime lifetime,
        ISplashService splashService, ILogger<UpdateService> logger)
    {
        SettingsService = settingsService;
        TaskBarIconService = taskBarIconService;
        SplashService = splashService;
        Logger = logger;

        var keyStream = AssetLoader.Open(new Uri("avares://ClassIsland/Assets/TrustedPublicKeys/ClassIsland.MetadataPublisher.asc", UriKind.RelativeOrAbsolute));
        MetadataPublisherPublicKey = new StreamReader(keyStream).ReadToEnd();

        RequestHelper = new WebRequestHelper(AppBase.Current.IsDevelopmentBuild 
                                             && !string.IsNullOrWhiteSpace(Settings.DebugPhainonRootUrlOverride) 
                                             && Uri.TryCreate(Settings.DebugPhainonRootUrlOverride, UriKind.Absolute, out var u1)
            ? u1
            : new Uri(PhainonRootUrl), true);
        _distributionInfo = ConfigureFileHelper.LoadConfig<DistributionInfoClient>(UpdateDistributionInfoPath);
        DistributionMetadata = ConfigureFileHelper.LoadConfig<DistributionMetadata>(UpdateDistributionMetadataPath);
    }

    public bool IsCanceled
    {
        get => _isCanceled;
        set => SetField(ref _isCanceled, value);
    }

    public Exception? NetworkErrorException
    {
        get => _networkErrorException;
        set => SetField(ref _networkErrorException, value);
    }

    public async Task<bool> AppStartup()
    {
        if (Settings.UpdateMode < 1)
        {
            return false;
        }
        
        _ = AppStartupBackground();
        return false;
    }

    private async Task AppStartupBackground()
    {
        if (Settings.LastUpdateStatus == UpdateStatus.UpdateDeployed)
        {
            CleanupPrevDeployments();
            await PlatformServices.DesktopToastService.ShowToastAsync("更新成功",
                $"应用已更新到 {AppBase.AppVersion}，点击以查看详细信息。", UpdateNotificationClickedCallback);
            Settings.LastUpdateStatus = UpdateStatus.UpToDate;
        }
        
        await CheckUpdateAsync();

        if (Settings.UpdateMode < 2)
        {
            return;
        }

        if (Settings.LastUpdateStatus == UpdateStatus.UpdateAvailable)
        {
            await DownloadUpdateAsync();
        }

        if (Settings.UpdateMode < 3)
        {
            return;
        }

        if (Settings.LastUpdateStatus == UpdateStatus.UpdateDownloaded)
        {
            await ExtractUpdateAsync();
        }
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
        if (!AllowedPackageTypes.Contains(AppBase.Current.PackagingType))
        {
            return;
        }
        var transaction = SentrySdk.StartTransaction("Get Update Info", "appUpdating.getMetadata");
        NetworkErrorException = null;
        try
        {
            var spanGetIndex = transaction.StartChild("getIndex");
            var subChannel = GetCurrentSubChannel();
            CurrentWorkingStatus = UpdateWorkingStatus.CheckingUpdates;
            DistributionMetadata = await RequestHelper.SaveJson<DistributionMetadata>(
                new Uri("api/v1/public/distributions/metadata", UriKind.Relative), UpdateDistributionMetadataPath);
            if (!DistributionMetadata.Channels.ContainsKey(Settings.SelectedUpdateChannelV3))
            {
                Settings.SelectedUpdateChannelV3 = DistributionMetadata.DefaultChannelId;
            }
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
                $"{Assembly.GetExecutingAssembly().GetName().Version} -> {latest.Version}" +Environment.NewLine+
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

    private string GetCurrentSubChannel()
    {
        return AppBase.Current.IsDevelopmentBuild && !string.IsNullOrWhiteSpace(Settings.DebugSubChannelOverride)
            ? Settings.DebugSubChannelOverride
            : AppBase.Current.AppSubChannel;
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
        if (!AllowedPackageTypes.Contains(AppBase.Current.PackagingType))
        {
            return;
        }
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
        if (_downloadCancellationTokenSource != null)
        {
            await _downloadCancellationTokenSource.CancelAsync();
        }

        try
        {
            var publicKey =
                AppBase.Current.IsDevelopmentBuild && !string.IsNullOrWhiteSpace(Settings.DebugPublicKeyOverride)
                    ? Settings.DebugPublicKeyOverride
                    : MetadataPublisherPublicKey;
            var valid = DetachedSignatureProcessor.VerifyDetachedSignature(DistributionInfo.FileMapJson,
                Encoding.UTF8.GetBytes(DistributionInfo.FileMapSignature), publicKey);
            if (!valid)
            {
                throw new InvalidOperationException("文件图签名校验不通过");
            }

            var fileMap = JsonSerializer.Deserialize<FileMap>(DistributionInfo.FileMapJson);
            if (fileMap == null)
            {
                throw new InvalidOperationException("文件图解析失败");
            }

            CurrentWorkingStatus = UpdateWorkingStatus.DownloadingUpdates;
            var cts = _downloadCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            var options = new DownloadConfiguration()
            {
                ChunkCount = 4,
                ParallelCount = 4,
                ParallelDownload = true,
                Timeout = 60_000
            };
            // key 是 hash（HEX）
            Dictionary<string, (string Name, FileMapFile FileInfo)> filesHashed = [];
            var deploymentLock = new DeploymentLock()
            {
                SubChannel = GetCurrentSubChannel(),
                FileMapSha512 = SHA512.HashData(Encoding.UTF8.GetBytes(DistributionInfo.FileMapJson))
            };

            Logger.LogTrace("正在计算要下载的文件");
            var prevFileMapPath = Path.Combine(Environment.CurrentDirectory, "files.json");
            var prevFileMap = new FileMap();
            if (File.Exists(prevFileMapPath))
            {
                try
                {
                    prevFileMap = ConfigureFileHelper.LoadConfigUnWrapped<FileMap>(prevFileMapPath, false);
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e, "无法加载当前版本的文件图 {}", prevFileMapPath);
                }
            }

            foreach (var (id, component) in fileMap.Components)
            {
                if (!Components.Contains(id))
                {
                    continue;
                }

                var prevComp = prevFileMap.Components.GetValueOrDefault(id);
                var existedFiles = new List<string>();
                deploymentLock.ExistedFiles[id] = existedFiles;

                foreach (var (path, file) in component.Files)
                {
                    if (component.AllowDiffUpdate &&
                        prevComp?.Files.GetValueOrDefault(path)?.FileSha512.SequenceEqual(file.FileSha512) == true)
                    {
                        existedFiles.Add(path);
                        Logger.LogTrace("SKIP {}/{}", id, path);
                        continue;
                    }

                    Logger.LogTrace("ADD {}/{}", id, path);
                    filesHashed.TryAdd(Convert.ToHexString(file.FileSha512), (Path.GetFileName(path), file));
                }
            }

            var dlRoot = UpdateTempPath;
            if (!Directory.Exists(dlRoot))
            {
                Directory.CreateDirectory(dlRoot);
            }

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 8,
                CancellationToken = cancellationToken
            };
            DownloadedCount = 0;
            DownloadingCountTotal = filesHashed.Count;
            Logger.LogInformation("开始下载更新，要下载 {} 个文件", filesHashed.Count);
            var downloadTasksMap = new Dictionary<string, DownloadTaskInfo>();
            DownloadTasks.Clear();
            DownloadTasks.AddRange(filesHashed.Select(x => new DownloadTaskInfo()
            {
                FileName = x.Value.Name,
                Key = x.Key
            }));
            foreach (var info in DownloadTasks)
            {
                downloadTasksMap[info.Key] = info;
            }

            await Parallel.ForEachAsync(filesHashed, parallelOptions, async (pair, token) =>
            {
                var (hashHex, (fileName, file)) = pair;
                var updateStopwatch = Stopwatch.StartNew();
                Logger.LogInformation("开始下载 {}({})", fileName, file.ArchiveDownloadUrl);
                var info = downloadTasksMap.GetValueOrDefault(hashHex) ?? DownloadTaskInfo.CreateEmpty();
                info.State = DownloadState.Downloading;
                await using var downloader = DownloadBuilder.New()
                    .WithConfiguration(options)
                    .WithUrl(file.ArchiveDownloadUrl)
                    .WithFileLocation(Path.Combine(dlRoot, hashHex[..2], hashHex))
                    .Build();
                var taskCompletionSource = new TaskCompletionSource();
                downloader.DownloadFileCompleted += (_, args) =>
                {
                    if (args.Error != null)
                    {
                        taskCompletionSource.SetException(args.Error);
                        return;
                    }

                    Dispatcher.UIThread.InvokeAsync(() => { DownloadedCount++; });
                    taskCompletionSource.SetResult();
                };
                downloader.DownloadProgressChanged += (_, e) =>
                {
                    if (updateStopwatch.ElapsedMilliseconds < 250)
                        return;
                    updateStopwatch.Restart();
                    var totalSize = e.TotalBytesToReceive;
                    var downloadedSize = e.ReceivedBytesSize;
                    var downloadSpeed = e.BytesPerSecondSpeed;

                    var eta = TimeSpanHelper.FromSecondsSafe(downloadSpeed == 0
                        ? 0
                        : (long)((totalSize - downloadedSize) / downloadSpeed));
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        info.FileSize = totalSize;
                        info.DownloadedSize = downloadedSize;
                        info.DownloadSpeed = downloadSpeed;
                        info.TimeToComplete = eta;
                    });
                };
                token.Register(() => { taskCompletionSource.SetCanceled(token); });
                await downloader.StartAsync(token);
                await taskCompletionSource.Task;
                info.State = DownloadState.Completed;
                // DownloadTasks.Remove(info);
                Logger.LogInformation("下载完成 {}({})", fileName, file.ArchiveDownloadUrl);
            });

            await File.WriteAllTextAsync(Path.Combine(UpdateTempPath, "FileMap.json"), DistributionInfo.FileMapJson,
                cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(UpdateTempPath, "FileMap.json.sig"),
                DistributionInfo.FileMapSignature, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(UpdateTempPath, "Deployment.lock"),
                JsonSerializer.Serialize(deploymentLock), cancellationToken);
            Logger.LogInformation("全部下载完成！");
            Settings.LastUpdateStatus = UpdateStatus.UpdateDownloaded;
        }
        catch (TaskCanceledException)
        {
            transaction.Finish(SpanStatus.Cancelled);
            Logger.LogInformation("已取消下载更新");
        }
        catch (Exception ex)
        {
            NetworkErrorException = ex;
            transaction.Finish(ex, SpanStatus.InternalError);
            Logger.LogError(ex, "下载应用更新失败");
            await RemoveDownloadedFiles(true);
        }
        finally
        {
            CurrentWorkingStatus = UpdateWorkingStatus.Idle;
            _downloadCancellationTokenSource = null;
        }
    }

    public async Task StopDownloading()
    {
        Logger.LogInformation("应用更新下载停止。");
        IsCanceled = true;
        if (_downloadCancellationTokenSource != null)
        {
            try
            {
                await _downloadCancellationTokenSource.CancelAsync();
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        _downloadCancellationTokenSource = null;
        CurrentWorkingStatus = UpdateWorkingStatus.Idle;
        await RemoveDownloadedFiles(true);
    }

    public async Task RemoveDownloadedFiles(bool isCancel)
    {
        try
        {
            Directory.Delete(UpdateTempPath, true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "移除下载临时文件失败。");
        }

        if (!isCancel)
        {
            return;
        }
        await CheckUpdateAsync(isCancel:true);
    }
    

    public async Task ExtractUpdateAsync()
    {
        if (!AllowedPackageTypes.Contains(AppBase.Current.PackagingType))
        {
            return;
        }
        CurrentWorkingStatus = UpdateWorkingStatus.ExtractingUpdates;
        try
        {
            Logger.LogInformation("正在部署应用更新");
            var fileMapJson = await File.ReadAllTextAsync(Path.Combine(UpdateTempPath, "FileMap.json"));
            var fileMapSig = await File.ReadAllTextAsync(Path.Combine(UpdateTempPath, "FileMap.json.sig"));
            var deploymentLock = ConfigureFileHelper.LoadConfigUnWrapped<DeploymentLock>(Path.Combine(UpdateTempPath, "Deployment.lock"), false);
            if (!deploymentLock.FileMapSha512.SequenceEqual(SHA512.HashData(Encoding.UTF8.GetBytes(fileMapJson))))
            {
                throw new InvalidOperationException("文件图哈希与下载时不符合，可能已经损坏");
            }
            if (deploymentLock.SubChannel != GetCurrentSubChannel())
            {
                throw new InvalidOperationException("下载的更新不适用于当前子频道的 ClassIsland");
            }
            var publicKey =
                AppBase.Current.IsDevelopmentBuild && !string.IsNullOrWhiteSpace(Settings.DebugPublicKeyOverride)
                    ? Settings.DebugPublicKeyOverride
                    : MetadataPublisherPublicKey;
            var valid = DetachedSignatureProcessor.VerifyDetachedSignature(fileMapJson,
                Encoding.UTF8.GetBytes(fileMapSig), publicKey);
            if (!valid)
            {
                throw new InvalidOperationException("文件图签名校验不通过");
            }

            var fileMap = JsonSerializer.Deserialize<FileMap>(fileMapJson);
            if (fileMap == null)
            {
                throw new InvalidOperationException("文件图解析失败");
            }

            Logger.LogInformation("正在解压并检验文件完整性");
            
            var root = CommonDirectories.AppPackageRoot;
            
            Logger.LogTrace("DeployRoot = {}", root);
            var extractedPath = Path.Combine(UpdateTempPath, "extracted");
            if (!Directory.Exists(extractedPath))
            {
                Directory.CreateDirectory(extractedPath);
            }
            
            foreach (var (id, component) in fileMap.Components)
            {
                if (!Components.Contains(id))
                {
                    continue;
                }

                var existedFiles = deploymentLock.ExistedFiles.GetValueOrDefault(id, []);

                foreach (var (path, fileInfo) in component.Files.Where(x =>
                             !component.AllowDiffUpdate || !existedFiles.Contains(x.Key)))
                {
                    var hashHex = Convert.ToHexString(fileInfo.FileSha512);
                    var fullPath = Path.Combine(extractedPath, hashHex[..2], hashHex);
                    if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("Path is null"));
                    }

                    Logger.LogTrace("正在解压：{}({})", path, fullPath);
                    await using (var file = File.OpenWrite(fullPath))
                    {
                        await using var archive = File.OpenRead(Path.Combine(UpdateTempPath, hashHex[..2], hashHex));
                        archive.Position = 0;
                        await using var gZipStream = new GZipStream(archive, CompressionMode.Decompress);
                        await gZipStream.CopyToAsync(file);
                    }

                    await using (var file = File.OpenRead(fullPath))
                    {
                        var hash = await SHA512.HashDataAsync(file);
                        if (!hash.SequenceEqual(fileInfo.FileSha512))
                        {
                            throw new InvalidOperationException($"文件 {path} 的 SHA512 校验失败 ");
                        }
                    }
                }
            }
            
            Logger.LogTrace("正在检查文件目录");

            var num = 0;
            fileMap.Variables["number"] = num.ToString();
            while (num <= 255 && Directory.Exists(Path.Combine(root,
                       VariableStringHelpers.ExpandString(fileMap.Components["app"].Root, fileMap.Variables))))
            {
                num++;
                fileMap.Variables["number"] = num.ToString();
            }
            
            foreach (var (id, component) in fileMap.Components)
            {
                if (!Components.Contains(id))
                {
                    continue;
                }
                
                var compRoot = Path.Combine(root,
                    VariableStringHelpers.ExpandString(component.Root, fileMap.Variables));

                if (component.Files.Any(x =>
                        Path.GetRelativePath(root, Path.Combine(compRoot, x.Key)).StartsWith("..") &&
                        !Path.IsPathRooted(Path.Combine(compRoot, x.Key))
                    ))
                {
                    throw new InvalidOperationException("文件图发现非法文件路径");
                }
            }

            Logger.LogInformation("正在准备部署");
            
            Logger.LogInformation("Variables: {}", JsonSerializer.Serialize(fileMap.Variables));

            var appPath = Path.Combine(root,
                VariableStringHelpers.ExpandString(fileMap.Components["app"].Root, fileMap.Variables));
            if (!Directory.Exists(appPath))
            {
                Directory.CreateDirectory(appPath);
                await File.WriteAllTextAsync(Path.Combine(appPath, ".partial"), "");
            }
            
            foreach (var (id, component) in fileMap.Components)
            {
                if (!Components.Contains(id))
                {
                    continue;
                }
                
                var existedFiles = deploymentLock.ExistedFiles.GetValueOrDefault(id, []);
                var compRoot = Path.Combine(root,
                    VariableStringHelpers.ExpandString(component.Root, fileMap.Variables));

                foreach (var (path, fileInfo) in component.Files)
                {
                    var targetPath = Path.Combine(compRoot, path);
                    var dir = Path.GetDirectoryName(targetPath);
                    if (dir != null && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    
                    if (component.AllowDiffUpdate && existedFiles.Contains(path) && id is "app")
                    {
                        var existedFileRoot = id switch
                        {
                            "app" => Environment.CurrentDirectory,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        var existedPath = Path.Combine(existedFileRoot, path);
                        
                        Logger.LogTrace("Deploy Copy EXISTED {} -> {}", existedPath, targetPath);
                        File.Copy(existedPath, targetPath, true);
                        continue;
                    }
                    
                    var hashHex = Convert.ToHexString(fileInfo.FileSha512);
                    var fullPath = Path.Combine(extractedPath, hashHex[..2], hashHex);
                    Logger.LogTrace("Deploy Copy {} -> {}", fullPath, targetPath);
                    File.Copy(fullPath, targetPath, true);
                }
            }

            if (OperatingSystem.IsLinux() && AppBase.Current.PackagingType == "folder")
            {
                using var proc = Process.Start(new ProcessStartInfo("chmod",
                [
                    "+x",
                    Path.GetFullPath(Path.Combine(root,
                        "ClassIsland"))
                ]));
                var task = proc?.WaitForExitAsync();
                if (task != null)
                {
                    await task;
                }
            }
            File.Copy(Path.Combine(UpdateTempPath, "FileMap.json"), Path.Combine(appPath, "files.json"));
            
            Logger.LogInformation("正在激活新的部署");
            
            await File.WriteAllTextAsync(Path.Combine(appPath, ".current"), "");
            await File.WriteAllTextAsync(Path.Combine(Environment.CurrentDirectory, ".destroy"), "");
            File.Delete(Path.Combine(appPath, ".partial"));
            foreach (var deployment in Directory.GetDirectories(root)
                         .Where(x => Path.GetFileName(x).StartsWith("app") 
                                     && Path.GetFullPath(Path.Combine(root, x)) != Path.GetFullPath(Environment.CurrentDirectory)
                                     && File.Exists(Path.Combine(x, ".current"))))
            {
                File.Delete(Path.Combine(deployment, ".current"));
            }

            Settings.LastUpdateStatus = UpdateStatus.UpdateDeployed;
            Logger.LogInformation("部署成功");
            await RemoveDownloadedFiles(false);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "无法部署应用更新");
            await RemoveDownloadedFiles(true);
        }
        finally
        {
            CurrentWorkingStatus = UpdateWorkingStatus.Idle;
        }
    }

    public void CleanupPrevDeployments()
    {
        var root = CommonDirectories.AppPackageRoot;
        foreach (var deployment in Directory.GetDirectories(root)
                     .Where(x => Path.GetFileName(x).StartsWith("app") 
                                 && Path.GetFullPath(Path.Combine(root, x)) != Path.GetFullPath(Environment.CurrentDirectory)
                                 && File.Exists(Path.Combine(x, ".destroy"))))
        {
            Logger.LogInformation("正在清理先前的部署：{}", deployment);
            try
            {
                Directory.Delete(Path.Combine(root, deployment), true);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "无法清理部署 {}", deployment);
            }
        }
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