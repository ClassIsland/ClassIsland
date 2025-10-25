using System;
using System.Collections.ObjectModel;
using ClassIsland.Core.Models.Logging;
using ClassIsland.Services.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace ClassIsland.ViewModels;

public partial class AppLogsViewModel : ObservableRecipient
{
    private ReadOnlyObservableCollection<LogEntry> _logs = null!;
    public ReadOnlyObservableCollection<LogEntry> Logs => _logs;
    public AppLogService AppLogService { get; }

    private IDisposable? _prevSubscription;

    [ObservableProperty] private string _filterText = "";
    [ObservableProperty] private ObservableCollection<int> _filterTypes = [];
    [ObservableProperty] private bool _isFilteredCritical = true;
    [ObservableProperty] private bool _isFilteredError = true;
    [ObservableProperty] private bool _isFilteredWarning = true;
    [ObservableProperty] private bool _isFilteredInfo = false;
    [ObservableProperty] private bool _isFilteredDebug = false;
    [ObservableProperty] private bool _isFilteredTrace = false;

    /// <inheritdoc/>
    public AppLogsViewModel(AppLogService appLogService)
    {
        AppLogService = appLogService;

        RefreshSource();

        this.ObservableForProperty(x => x.IsFilteredCritical).Subscribe(_ => RefreshSource());
        this.ObservableForProperty(x => x.IsFilteredDebug).Subscribe(_ => RefreshSource());
        this.ObservableForProperty(x => x.IsFilteredError).Subscribe(_ => RefreshSource());
        this.ObservableForProperty(x => x.IsFilteredInfo).Subscribe(_ => RefreshSource());
        this.ObservableForProperty(x => x.IsFilteredTrace).Subscribe(_ => RefreshSource());
        this.ObservableForProperty(x => x.IsFilteredWarning).Subscribe(_ => RefreshSource());
        this.ObservableForProperty(x => x.FilterText).Subscribe(_ => RefreshSource());

    }

    private void RefreshSource()
    {
        _prevSubscription?.Dispose();
        var observable = AppLogService.Logs
            .Connect()
            .Filter(x =>
            {
                bool c1 = (IsFilteredCritical && x.LogLevel == LogLevel.Critical) ||
                          (IsFilteredError && x.LogLevel == LogLevel.Error) ||
                          (IsFilteredWarning && x.LogLevel == LogLevel.Warning) ||
                          (IsFilteredInfo && x.LogLevel == LogLevel.Information) ||
                          (IsFilteredDebug && x.LogLevel == LogLevel.Debug) ||
                          (IsFilteredTrace && x.LogLevel == LogLevel.Trace);
                if (string.IsNullOrWhiteSpace(FilterText))
                {
                    return c1;
                }
                else
                {
                    return c1 && (x.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                                  x.CategoryName.Contains(FilterText));
                }
            })
            .Bind(out _logs);
        OnPropertyChanged(nameof(Logs));
        _prevSubscription = observable.Subscribe();
    }
}