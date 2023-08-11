using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ClassIsland.Enums;
using ClassIsland.Models;
using Downloader;
using IWshRuntimeLibrary;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Hosting;
using Microsoft.Toolkit.Uwp.Notifications;
using DownloadProgressChangedEventArgs = Downloader.DownloadProgressChangedEventArgs;
using File = System.IO.File;

namespace ClassIsland;

public class UpdateService : BackgroundService, INotifyPropertyChanged
{
    private UpdateWorkingStatus _currentWorkingStatus = UpdateWorkingStatus.Idle;
    private long _downloadedSize = 0;
    private long _totalSize = 0;
    private double _downloadSpeed = 0;
    public IDownload? Downloader;
    private bool _isCanceled = false;
    private Exception? _networkErrorException;

    public string CurrentUpdateSourceUrl => Settings.SelectedChannel;

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

    private TaskBarIconService TaskBarIconService
    {
        get;
    }

    public UpdateService(SettingsService settingsService, TaskBarIconService taskBarIconService)
    {
        SettingsService = settingsService;
        TaskBarIconService = taskBarIconService;
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
    }

    public static async Task<List<AppCenterReleaseInfoMin>> GetUpdateVersionsAsync(string queryRoot)
    {
        var http = new HttpClient();
        var json = await http.GetStringAsync(queryRoot);
        var o = JsonSerializer.Deserialize<List<AppCenterReleaseInfoMin>>(json);
        if (o != null)
        {
            return o;
        }
        throw new ArgumentException("Releases info array is null!");
    }

    public static async Task<AppCenterReleaseInfo> GetVersionArtifactsAsync(string versionRoot)
    {
        var http = new HttpClient();
        var json = await http.GetStringAsync(versionRoot);
        var o = JsonSerializer.Deserialize<AppCenterReleaseInfo>(json);
        if (o != null)
        {
            return o;
        }
        throw new ArgumentException("Release package info array is null!");
    }

    public async Task CheckUpdateAsync()
    {
        try
        {

            CurrentWorkingStatus = UpdateWorkingStatus.CheckingUpdates;
            var versions = await GetUpdateVersionsAsync(CurrentUpdateSourceUrl + "/public_releases");
            if (versions.Count <= 0)
            {
                CurrentWorkingStatus = UpdateWorkingStatus.Idle;
                return;
            }

            var v = (from i in versions orderby i.UploadTime select i).Reverse().ToList()[0]!;
            var verCode = Version.Parse(v.Version);
            if (verCode > Assembly.GetExecutingAssembly().GetName().Version)
            {
                Settings.LastUpdateStatus = UpdateStatus.UpdateAvailable;
                Settings.LastCheckUpdateInfoCache =
                    await GetVersionArtifactsAsync(CurrentUpdateSourceUrl + $"/releases/{v.Id}");
                TaskBarIconService.TaskBarIcon.ShowNotification("发现新版本",
                    $"{Assembly.GetExecutingAssembly().GetName().Version} -> {verCode}\n" +
                    "点击以查看详细信息。");
            }

            Settings.LastCheckUpdateTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            Settings.LastUpdateStatus = UpdateStatus.UpToDate;
            NetworkErrorException = ex;
        }
        finally
        {
            CurrentWorkingStatus = UpdateWorkingStatus.Idle;
        }
    }

    public async Task DownloadUpdateAsync()
    {
        try
        {
            TotalSize = 0;
            DownloadedSize = 0;
            DownloadSpeed = 0;
            CurrentWorkingStatus = UpdateWorkingStatus.DownloadingUpdates;

            Downloader = DownloadBuilder.New()
                .WithUrl(Settings.LastCheckUpdateInfoCache.DownloadUrl)
                .Configure((c) =>
                {
                    c.ParallelCount = 32;
                    c.ParallelDownload = true;
                })
                .WithDirectory(@".\UpdateTemp")
                .WithFileName("update.zip")
                .Build();
            Downloader.DownloadProgressChanged += DownloaderOnDownloadProgressChanged;
            await Downloader.StartAsync();
            if (IsCanceled)
            {
                IsCanceled = false;
                return;
            }

            Settings.LastUpdateStatus = UpdateStatus.UpdateDownloaded;

        }
        catch (Exception ex)
        {
            NetworkErrorException = ex;
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
        catch
        {
            // ignored
        }
        await CheckUpdateAsync();
    }

    private void DownloaderOnDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        TotalSize = e.TotalBytesToReceive;
        DownloadedSize = e.ReceivedBytesSize;
        DownloadSpeed = e.BytesPerSecondSpeed;
    }

    public async Task ExtractUpdateAsync()
    {
        CurrentWorkingStatus = UpdateWorkingStatus.ExtractingUpdates;
        await Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(@"./UpdateTemp/update.zip", "./UpdateTemp/extracted", true);
        });
    }

    public async Task RestartAppToUpdateAsync()
    {
        await ExtractUpdateAsync();
        Application.Current.Shutdown();
        Process.Start(new ProcessStartInfo()
        {
            FileName = "./UpdateTemp/extracted/ClassIsland.exe",
            ArgumentList =
            { 
                "-urt", Environment.ProcessPath!,
                "-m", "true"
            }
        });
        App.ReleaseLock();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => null;

    public override async Task StartAsync(CancellationToken cancellationToken)
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