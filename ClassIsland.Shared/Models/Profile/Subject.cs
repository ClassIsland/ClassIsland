namespace ClassIsland.Shared.Models.Profile;

/// <summary>
/// 代表一个科目
/// </summary>
public class Subject : AttachableSettingsObject
{
    private string _name = "";
    private string _initial = "";
    private string _teacherName = "";
    private bool _isOutDoor = false;

    /// <summary>
    /// 科目名
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
    /// 科目简称
    /// </summary>
    public string Initial
    {
        get => _initial;
        set
        {
            if (value == _initial) return;
            _initial = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 教师名
    /// </summary>
    public string TeacherName
    {
        get => _teacherName;
        set
        {
            if (value == _teacherName) return;
            _teacherName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否为户外课程
    /// </summary>
    public bool IsOutDoor
    {
        get => _isOutDoor;
        set
        {
            if (value == _isOutDoor) return;
            _isOutDoor = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 代表一个空科目
    /// </summary>
    public static readonly Subject Empty = new()
    {
        Initial = "?",
        Name = "???"
    };
}