namespace ClassIsland.Core.Models.Profile;

public class Subject : AttachableSettingsObject
{
    private string _name = "";
    private string _initial = "";
    private string _teacherName = "";
    private bool _isOutDoor = false;

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

    public static readonly Subject Empty = new()
    {
        Initial = "?",
        Name = "???"
    };
}