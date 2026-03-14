using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Tutorial;

/// <summary>
/// 代表一个教程中的段落。
/// </summary>
public partial class TutorialParagraph : ObservableObject, IXmlnsAttached
{
    /// <summary>
    /// 段落 ID
    /// </summary>
    [ObservableProperty] private string _id = "";

    /// <summary>
    /// 段落标题
    /// </summary>
    [ObservableProperty] private string _title = "";

    /// <summary>
    /// 段落描述
    /// </summary>
    [ObservableProperty] private string _description = "";
    
    /// <summary>
    /// 此段落教程附加的 <see cref="TopLevel"/> 类名称
    /// </summary>
    [ObservableProperty] private string _topLevelClassName = "";
    
    /// <summary>
    /// 初始化动作
    /// </summary>
    [ObservableProperty] private ObservableCollection<TutorialAction> _initializeActions = [];

    /// <summary>
    /// 教程语句列表
    /// </summary>
    [ObservableProperty] private ObservableCollection<TutorialSentence> _content = [];
    
    [ObservableProperty] private IDictionary<string, string> _xmlns = new Dictionary<string, string>();
    
}