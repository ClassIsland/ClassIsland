using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using static System.Windows.Forms.AxHost;

namespace ClassIsland.Core.Models.Ruleset;

/// <summary>
/// 代表一个包含若干个规则的规则集。要判定规则集是否生效，需要使用<see cref="IRulesetService"/>。
/// </summary>
public class Ruleset : ObservableRecipient
{
    private RulesetLogicalMode _mode = RulesetLogicalMode.Or;
    private bool _isReversed = false;
    private ObservableCollection<RuleGroup> _groups = [new() { Rules = [new()] }];
    private int _state = 0;

    /// <summary>
    /// 逻辑模式。
    /// </summary>
    public RulesetLogicalMode Mode
    {
        get => _mode;
        set
        {
            if (value == _mode) return;
            _mode = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否反转判断。
    /// </summary>
    public bool IsReversed
    {
        get => _isReversed;
        set
        {
            if (value == _isReversed) return;
            _isReversed = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 规则分组
    /// </summary>
    public ObservableCollection<RuleGroup> Groups
    {
        get => _groups;
        set
        {
            if (Equals(value, _groups)) return;
            _groups = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 满足状态
    /// </summary>
    [JsonIgnore]
    public int State
    {
        get => _state;
        set
        {
            if (value == _state) return;
            _state = value;
            OnPropertyChanged();
        }
    }
}