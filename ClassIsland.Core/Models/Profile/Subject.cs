using System.Text.RegularExpressions;

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

    public string GetFirstName()
    {
        if (string.IsNullOrWhiteSpace(TeacherName))
        {
            return string.Empty;
        }

        // 判断是否包含中文字符
        var containsChinese = Regex.IsMatch(TeacherName, @"[\u4e00-\u9fa5]");

        if (containsChinese)
        {
            // 中文姓名，假设姓氏为第一个字符
            if (TeacherName.Length >= 1) return TeacherName[..1];
        }
        else
        {
            // 英文姓名，假设姓氏为最后一个单词
            var nameParts = TeacherName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return nameParts.Length > 1 ? nameParts[^1] : TeacherName; // 处理只有一个单词的情况
        }

        return "";
    }
}