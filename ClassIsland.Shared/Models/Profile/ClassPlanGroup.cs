using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 代表一个课表群。
/// </summary>
public class ClassPlanGroup : ObservableRecipient
{
    private string _name = "新课表群";
    private bool _isGlobal = false;

    /// <summary>
    /// 课表群名称。
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
    /// 课表群是否为全局课表群
    /// </summary>
    public bool IsGlobal
    {
        get => _isGlobal;
        set
        {
            if (value == _isGlobal) return;
            _isGlobal = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 默认课表群 GUID。
    /// </summary>
    public static Guid DefaultGroupGuid { get; } = new("ACAF4EF0-E261-4262-B941-34EA93CB4369");

    /// <summary>
    /// 全局课表群 GUID。
    /// </summary>
    public static Guid GlobalGroupGuid { get; } = Guid.Empty;
}