using System.Collections.ObjectModel;
using ClassIsland.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Ruleset;

/// <summary>
/// 代表一个规则组。
/// </summary>
public class RuleGroup : ObservableRecipient
{
    private ObservableCollection<Rule> _rules = new();
    private RulesetLogicalMode _mode = RulesetLogicalMode.And;
    private bool _isReversed = false;
    private bool _isEnabled = true;

    /// <summary>
    /// 规则条目。
    /// </summary>
    public ObservableCollection<Rule> Rules
    {
        get => _rules;
        set
        {
            if (Equals(value, _rules)) return;
            _rules = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 判断模式。
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
    /// 是否启用规则集。
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
}