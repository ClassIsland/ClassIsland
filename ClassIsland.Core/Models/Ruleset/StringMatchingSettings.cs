using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Ruleset;

/// <summary>
/// 代表一个字符匹配规则配置。
/// </summary>
public class StringMatchingSettings : ObservableRecipient
{
    private string _text = "";
    private bool _useRegex = false;

    /// <summary>
    /// 要匹配的字符串/正则。
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (value == _text) return;
            _text = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否使用正则。
    /// </summary>
    public bool UseRegex
    {
        get => _useRegex;
        set
        {
            if (value == _useRegex) return;
            _useRegex = value;
            OnPropertyChanged();
        }
    }
}