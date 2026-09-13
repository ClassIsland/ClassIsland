using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 代表一组科目。
/// </summary>
public class SubjectGroup : ObservableRecipient
{
    private string _name = "新科目分组";

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
}
