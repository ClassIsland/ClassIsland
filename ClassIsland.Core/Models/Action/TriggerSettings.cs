using System.Text.Json.Serialization;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Action;

/// <summary>
/// 代表一个触发器的设置。
/// </summary>
public class TriggerSettings : ObservableRecipient
{
    private string _id = "";
    private object? _settings;

    /// <summary>
    /// 触发器 ID
    /// </summary>
    public string Id
    {
        get => _id;
        set
        {
            if (value == _id) return;
            _id = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AssociatedTriggerInfo));
        }
    }

    /// <summary>
    /// 触发器设置
    /// </summary>
    public object? Settings
    {
        get => _settings;
        set
        {
            if (Equals(value, _settings)) return;
            _settings = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 关联的触发器信息
    /// </summary>
    [JsonIgnore]
    public TriggerInfo? AssociatedTriggerInfo => IAutomationService.RegisteredTriggers.FirstOrDefault(x => x.Id == Id);

    [JsonIgnore]
    internal TriggerBase? TriggerInstance { get; set; }

    internal void Unload()
    {
        Unloading?.Invoke(this, EventArgs.Empty);
    }

    internal event EventHandler? Unloading;
}