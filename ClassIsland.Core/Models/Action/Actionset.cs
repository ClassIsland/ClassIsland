using CommunityToolkit.Mvvm.ComponentModel;
using ClassIsland.Core.Abstractions.Services;
using System.Collections.ObjectModel;
namespace ClassIsland.Core.Models.Action;

/// <summary>
/// 代表一个行动组。要触发或恢复行动组，需要使用<see cref="IActionService"/>。
/// </summary>
public class Actionset : ObservableRecipient
{
    private bool _isEnabled = true;
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (value == _isEnabled) return;
            _isEnabled = value;
            OnPropertyChanged();
        }
    }

    private string _name = "新行动";
    /// <summary>
    /// “名称”（自动化）/ “备注”（行动组）
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (value == _name) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    private string _guid = System.Guid.NewGuid().ToString();
    /// <summary>
    /// 行动组Guid，仅用于标识设置叠层。
    /// </summary>
    public string Guid
    {
        get => _guid;
        set
        {
            if (value == _guid) return;
            _guid = value;
            OnPropertyChanged();
        }
    }

    private bool _isOn = false;
    /// <summary>
    /// 行动组被触发后还未恢复。
    /// </summary>
    public bool IsOn
    {
        get => _isOn;
        set
        {
            if (value == _isOn) return;
            _isOn = value;
            OnPropertyChanged();
        }
    }

    private ObservableCollection<Action> _actions = [];
    /// <summary>
    /// 行动组中的所有行动。
    /// </summary>
    public ObservableCollection<Action> Actions
    {
        get => _actions;
        set
        {
            if (value == _actions) return;
            _actions = value;
            OnPropertyChanged();
        }
    }
}