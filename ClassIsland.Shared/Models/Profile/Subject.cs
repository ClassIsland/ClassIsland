using System.Text.RegularExpressions;

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
    private static readonly HashSet<string> commonCompoundSurnames = new() { "万俟", "司马", "上官", "欧阳", "夏侯", "诸葛", "闻人", "东方", "赫连", "皇甫", "尉迟", "公羊", "澹台", "公冶", "宗政", "濮阳", "淳于", "单于", "太叔", "申屠", "公孙", "仲孙", "轩辕", "令狐", "钟离", "宇文", "长孙", "慕容", "鲜于", "闾丘", "司徒", "司空", "亓官", "司寇", "子车", "颛孙", "端木", "巫马", "公西", "漆雕", "乐正", "壤驷", "公良", "拓跋", "夹谷", "宰父", "谷梁", "段干", "百里", "东郭", "南门", "呼延", "羊舌", "微生", "梁丘", "左丘", "东门", "西门", "南宫", "第五" };

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
            if (string.IsNullOrEmpty(Initial) && !string.IsNullOrWhiteSpace(Name))
                Initial = Name.First().ToString();
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
    /// 代表一个空科目。
    /// </summary>
    public static readonly Subject Empty = new()
    {
        Initial = "?",
        Name = "???"
    };

    /// <summary>
    /// 代表一个课间休息科目。
    /// </summary>
    public static readonly Subject Breaking = new()
    {
        Initial = "休",
        Name = "课间休息"
    };

    /// <summary>
    /// 获取当前科目任课老师姓名的姓氏，如果没有填写任课老师信息，则返回空字符串。
    /// </summary>
    /// <returns>任课老师的姓氏。</returns>
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
            // 中文姓名，处理复姓
            if (TeacherName.Length >= 2)
            {
                var teacherSurname = TeacherName.Substring(0, 2);
                if (commonCompoundSurnames.Contains(teacherSurname))
                {
                    return teacherSurname;
                }
            }

            // 中文姓名，假设姓氏为第一个字符
            if (TeacherName.Length >= 1) return TeacherName.Substring(0, 1);
        }
        else
        {
            // 英文姓名，假设姓氏为最后一个单词
            var nameParts = TeacherName.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            return nameParts.Length > 1 ? nameParts[nameParts.Length - 1] : TeacherName; // 处理只有一个单词的情况
        }

        return "";
    }
}