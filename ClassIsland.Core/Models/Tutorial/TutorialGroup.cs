using System.Collections.ObjectModel;
using ClassIsland.Core.Abstractions.Models;
using ClassIsland.Core.Attributes;
using CommunityToolkit.Mvvm.ComponentModel;
using MoonSharp.Interpreter;

namespace ClassIsland.Core.Models.Tutorial;

/// <summary>
/// 代表一组教程。
/// </summary>
[MoonSharpUserData]
public partial class TutorialGroup : ObservableObject, IXmlnsAttached
{
    /// <summary>
    /// 章节 ID
    /// </summary>
    [ObservableProperty] private string _id = "";
    
    /// <summary>
    /// 章节标题
    /// </summary>
    [ObservableProperty] private string _title = "";

    /// <summary>
    /// 章节描述
    /// </summary>
    [ObservableProperty] private string _description = "";

    /// <summary>
    /// 此章节包含的教程。
    /// </summary>
    [ObservableProperty] private ObservableCollection<Tutorial> _tutorials = [];

    /// <summary>
    /// 贡献者信息。
    /// </summary>
    [ObservableProperty] ContributorInfo? _contributorInfo;
    
    [ObservableProperty] private IDictionary<string, string> _xmlns = new Dictionary<string, string>();
}