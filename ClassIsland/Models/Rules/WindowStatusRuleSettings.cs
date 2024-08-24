using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.Rules;

public class WindowStatusRuleSettings : ObservableRecipient
{
    private int _state = 1;

    /// <summary>
    /// 窗口状态。
    /// </summary>
    /// <value>
    /// 0 - 正常
    /// 1 - 最大化
    /// 2 - 全屏
    /// 3 - 最小化
    /// </value>
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