using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 档案中用于组织科目的可持久化分组。
/// </summary>
public class SubjectGroup : ObservableRecipient
{
    private string _name = "新科目分组";
    private string _color = "";

    /// <summary>
    /// 在科目选择器中显示的分组名称。
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

    /// <summary>
    /// 科目选择器中分组标题使用的颜色，采用 ARGB 十六进制格式；空值表示使用当前系统强调色。
    /// </summary>
    public string Color
    {
        get => _color;
        set
        {
            if (value == _color) return;
            _color = value;
            OnPropertyChanged();
        }
    }
}
