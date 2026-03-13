using System.Collections.ObjectModel;
using ClassIsland.Core.Abstractions.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Core.Models.Tutorial;

/// <summary>
/// 代表一个教程条目。
/// </summary>
public partial class Tutorial : ObservableObject, IXmlnsAttached
{
    /// <summary>
    /// 教程 ID
    /// </summary>
    [ObservableProperty] private string _id = "";
    
    /// <summary>
    /// 教程标题
    /// </summary>
    [ObservableProperty] private string _title = "";

    /// <summary>
    /// 教程描述
    /// </summary>
    [ObservableProperty] private string _description = "";

    /// <summary>
    /// 教程排序
    /// </summary>
    [ObservableProperty] private int _ordinary = 0;

    /// <summary>
    /// 教程 Banner
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActualBanner))]
    private string _banner = "";

    /// <summary>
    /// 实际启用的 Banner
    /// </summary>
    public string ActualBanner => string.IsNullOrWhiteSpace(Banner)
        ? "avares://ClassIsland/Assets/DefaultTutorialBanner.webp"
        : Banner;

    /// <summary>
    /// 包含的段落
    /// </summary>
    [ObservableProperty] private ObservableCollection<TutorialParagraph> _paragraphs = [];
    
    [ObservableProperty] private IDictionary<string, string> _xmlns = new Dictionary<string, string>();
}