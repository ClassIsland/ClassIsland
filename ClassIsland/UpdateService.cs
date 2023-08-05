using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Enums;
using ClassIsland.Models;
using Microsoft.Extensions.Hosting;

namespace ClassIsland;

public class UpdateService : BackgroundService, INotifyPropertyChanged
{
    private UpdateWorkingStatus _currentWorkingStatus = UpdateWorkingStatus.Idle;

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

    public UpdateService(SettingsService settingsService)
    {
        SettingsService = settingsService;
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
            Settings.LastCheckUpdateInfoCache = await GetVersionArtifactsAsync(CurrentUpdateSourceUrl + $"/releases/{v.Id}");
            Settings.LastCheckUpdateTime = DateTime.Now;
        }
        CurrentWorkingStatus = UpdateWorkingStatus.Idle;
    }

    public async Task DownloadUpdateAsync()
    {
        
    }

    public async Task ExtractUpdateAsync()
    {

    }

    public async Task RestartAppToUpdateAsync()
    {

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