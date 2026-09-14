using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 代表一组科目。
/// </summary>
public class SubjectGroup : ObservableRecipient
{
    private string _name = "新科目分组";
    private string _color = "#FF1E90FF";

    /// <summary>
    /// 科目分组名称。
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
    /// 科目选择器中分组标题使用的颜色，采用 ARGB 十六进制格式。
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
