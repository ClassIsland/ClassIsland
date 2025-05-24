using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Services.Registry;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Models;
using ClassIsland.Shared.Models.Notification;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationRequest = ClassIsland.Core.Models.Notification.NotificationRequest;

namespace ClassIsland.Services;

/// <summary>
/// 提醒主机服务。
/// </summary>
public class NotificationHostService(SettingsService settingsService, ILogger<NotificationHostService> logger)
    : IHostedService, INotifyPropertyChanged, INotificationHostService
{
    private SettingsService SettingsService { get; } = settingsService;
    private ILogger<NotificationHostService> Logger { get; } = logger;
    private Settings Settings => SettingsService.Settings;

    public PriorityQueue<NotificationRequest, NotificationPriority> RequestQueue { get; } = new();

    private int _queueIndex = 0;

    public ObservableCollection<NotificationProviderRegisterInfo> NotificationProviders { get; } = new();

    #region Events

    public event EventHandler? UpdateTimerTick;
    public void OnUpdateTimerTick(object sender, EventArgs args) => UpdateTimerTick?.Invoke(sender, args);
    
    public event EventHandler? OnClass;
    public void OnOnClass(object sender, EventArgs args) => OnClass?.Invoke(sender, args);

    public event EventHandler? OnBreakingTime;
    public void OnOnBreakingTime(object sender, EventArgs args) => OnBreakingTime?.Invoke(sender, args);

    public event EventHandler? CurrentStateChanged;
    public void OnCurrentStateChanged(object sender, EventArgs args) => CurrentStateChanged?.Invoke(sender, args);

    #endregion

    public NotificationRequest? CurrentRequest { get; set; }

    public NotificationRequest GetRequest()
    {
        CurrentRequest = RequestQueue.Dequeue();
        return CurrentRequest;
    }

    /// <summary>
    /// 注册提醒服务。
    /// </summary>
    /// <param name="provider">要注册的服务实例。</param>
    /// <example>
    /// <code>
    /// NotificationHostService.RegisterNotificationProvider(this);
    /// </code>
    /// </example>
    public void RegisterNotificationProvider(INotificationProvider provider)
    {
        Logger.LogInformation("注册提醒提供方：{}（{}）", provider.ProviderGuid, provider.Name);
        if (!Settings.NotificationProvidersPriority.Contains(provider.ProviderGuid.ToString()))
        {
            Settings.NotificationProvidersPriority.Add(provider.ProviderGuid.ToString());
        }
        if (!Settings.NotificationProvidersSettings.ContainsKey(provider.ProviderGuid.ToString()))
        {
            Settings.NotificationProvidersSettings.Add(provider.ProviderGuid.ToString(), null);
        }
        if (!Settings.NotificationProvidersEnableStates.ContainsKey(provider.ProviderGuid.ToString()))
        {
            Settings.NotificationProvidersEnableStates.Add(provider.ProviderGuid.ToString(), true);
        }
        if (!Settings.NotificationProvidersNotifySettings.ContainsKey(provider.ProviderGuid.ToString()))
        {
            Settings.NotificationProvidersNotifySettings.Add(provider.ProviderGuid.ToString(), new());
        }

        NotificationProviders.Add(new NotificationProviderRegisterInfo(provider)
        {
            ProviderSettings = Settings.NotificationProvidersNotifySettings[provider.ProviderGuid.ToString()]
        });

        if (provider is not NotificationProviderBase providerBase)
        {
            return;
        }

        var providerInfo = NotificationProviderRegistryService.RegisteredProviders.First(x => x.Guid == provider.ProviderGuid);
        foreach (var channelInfo in providerInfo.RegisteredChannels)
        {
            providerBase.Channels[channelInfo.Guid] = new NotificationChannel(providerBase, providerInfo, channelInfo);
        }
    }

    [Obsolete]
    public void ShowNotification(Shared.Models.Notification.NotificationRequest request)
    {
        var trace = new StackTrace();
        Logger.LogDebug("准备显示提醒\n{}", trace);
        foreach (var i in trace.GetFrames())
        {
            var type = i.GetMethod()?.DeclaringType;
            if (type?.IsAssignableTo(typeof(INotificationProvider)) != true)
                continue;
            var provider = (from p in NotificationProviders where p.ProviderInstance.GetType() == type select p).FirstOrDefault();
            if (provider == null)
                continue;
            Logger.LogInformation("请求来源：{}", provider.ProviderGuid);
            var newRequest = NotificationRequest.ConvertFromOldNotificationRequest(request);
            Logger.LogWarning("提醒提供方 {} 当前调用的提醒 API 已弃用，请使用 v2 提醒 API", provider.ProviderGuid);
            ShowNotification(newRequest, provider.ProviderGuid, Guid.Empty);
            return;
        }

        throw new ArgumentException("此方法只能由 INotificationProvider 调用。");
    }

    public void ShowNotification(NotificationRequest request, Guid providerGuid, Guid channelGuid)
    {
        request.NotificationSourceGuid = providerGuid;
        request.NotificationSource = (from i in NotificationProviders where i.ProviderGuid == providerGuid select i)
            .FirstOrDefault();
        request.ProviderSettings = request.NotificationSource?.ProviderSettings ?? request.ProviderSettings;
        if (_queueIndex +1 >= int.MaxValue)
        {
            _queueIndex = 0;
        }

        if (channelGuid != Guid.Empty && request.ChannelId == Guid.Empty)
        {
            request.ChannelId = channelGuid;
        }

        channelGuid = request.ChannelId;

        var channel =
            request.NotificationSource?.NotificationChannels.FirstOrDefault(x => x.ProviderGuid == channelGuid);
        request.ChannelSettings = channel?.ProviderSettings;
        RequestQueue.Enqueue(request, new NotificationPriority(Settings.NotificationProvidersPriority.IndexOf(providerGuid.ToString()), _queueIndex++, request.IsPriorityOverride) );
    }

    [Obsolete]
    public async Task ShowNotificationAsync(Shared.Models.Notification.NotificationRequest request)
    {
        ShowNotification(request);
        await Task.Run(() =>
        {
            request.CompletedTokenSource.Token.WaitHandle.WaitOne();
        });
    }

    public async Task ShowNotificationAsync(NotificationRequest request, Guid providerGuid, Guid channelGuid)
    {
        ShowNotification(request, providerGuid, channelGuid);
        await Task.Run(() =>
        {
            request.CompletedTokenSource.Token.WaitHandle.WaitOne();
        });
    }

    public void ShowChainedNotifications(NotificationRequest[] requests, Guid providerGuid, Guid channelGuid)
    {
        if (requests.Length <= 0)
        {
            return;
        }

        var rootCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(requests.Select(x => x.CancellationTokenSource.Token).ToArray());
        var rootCompletedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(requests.Select(x => x.CompletedTokenSource.Token).ToArray());
        rootCancellationTokenSource.Token.Register(() =>
        {
            foreach (var request in requests.Where(x => !x.CancellationToken.IsCancellationRequested))
            {
                request.CancellationTokenSource.Cancel();
            }
        });
        foreach (var request in requests)
        {
            request.RootCancellationTokenSource = rootCancellationTokenSource;
            request.RootCompletedTokenSource = rootCompletedTokenSource;
            
            ShowNotification(request, providerGuid, channelGuid);
        }
    }

    public async Task ShowChainedNotificationsAsync(NotificationRequest[] requests, Guid providerGuid, Guid channelGuid)
    {
        if (requests.Length <= 0)
        {
            return;
        }
        ShowChainedNotifications(requests, providerGuid, channelGuid);
        await Task.Run(() =>
        {
            requests.Last().CompletedTokenSource.Token.WaitHandle.WaitOne();
        });
    }

    public void RegisterNotificationChannel(NotificationChannel channel)
    {
        Logger.LogInformation("注册提醒渠道：{}（{}）", channel.ChannelInfo.Guid, channel.ChannelInfo.Name);
        if (!Settings.NotificationChannelsNotifySettings.ContainsKey(channel.ChannelInfo.Guid.ToString()))
        {
            Settings.NotificationChannelsNotifySettings.Add(channel.ChannelInfo.Guid.ToString(), new());
        }
        NotificationProviders.FirstOrDefault(x => x.ProviderGuid == channel.ChannelInfo.AssociatedProviderGuid)?.NotificationChannels.Add(new NotificationChannelRegisterInfo(channel)
        {
            ProviderSettings = Settings.NotificationChannelsNotifySettings[channel.ChannelInfo.Guid.ToString()]
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return new Task(()=>{});
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return new Task(()=>{});
    }

    /// <summary>
    /// 获取提醒服务实例。
    /// </summary>
    /// <typeparam name="T">提醒服务类型</typeparam>
    /// <param name="id">提醒服务id</param>
    /// <returns>对应提醒服务实例。若不存在，则返回null。</returns>
    public T GetNotificationProviderSettings<T>(Guid id) where T : class
    {
        Logger.LogInformation("获取提醒提供方设置：{}", id);
        var o = Settings.NotificationProvidersSettings[id.ToString()];
        var settings = o switch
        {
            JsonElement json => json.Deserialize<T>() ?? Activator.CreateInstance<T>(),
            T s => s,
            _ => Activator.CreateInstance<T>()
        };
        Settings.NotificationProvidersSettings[id.ToString()] = settings;
        return settings;
    }

    public void WriteNotificationProviderSettings<T>(Guid id, T settings)
    {
        Logger.LogInformation("写入提醒提供方设置：{}", id);
        Settings.NotificationProvidersSettings[id.ToString()] = settings;
    }

    public void CancelAllNotifications()
    {
        CurrentRequest?.CancellationTokenSource.Cancel();
        while (RequestQueue.Count > 0)
        {
            var r = RequestQueue.Dequeue();
            r.CompletedTokenSource.Cancel();
        }
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