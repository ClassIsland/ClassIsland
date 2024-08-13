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
    private RulesetLogicalMode _mode = RulesetLogicalMode.Or;
    private bool _isReversed = false;
    private bool _isEnabled = true;

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