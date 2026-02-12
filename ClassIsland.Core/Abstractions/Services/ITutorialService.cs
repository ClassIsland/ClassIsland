using System.Collections.ObjectModel;
using System.ComponentModel;
using ClassIsland.Core.Models.Tutorial;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>
/// 教程服务。
/// </summary>
public interface ITutorialService : INotifyPropertyChanged
{
    /// <summary>
    /// 已注册的教程组
    /// </summary>
    public static ObservableCollection<TutorialGroup> RegisteredTutorialGroups { get; } = [];
    
    /// <summary>
    /// 当前正在进行的教程
    /// </summary>
    Tutorial? CurrentTutorial { get; }
    
    /// <summary>
    /// 当前正在进行的教程段落
    /// </summary>
    TutorialParagraph? CurrentParagraph { get; }
    
    /// <summary>
    /// 当前正在进行的教程句
    /// </summary>
    TutorialSentence? CurrentSentence { get; }

    /// <summary>
    /// 开始指定的教学。
    /// </summary>
    /// <remarks>
    /// 仅在当前没有教学进行时生效。当当前有教学进行时，调用此方法不会有任何作用。
    /// </remarks>
    /// <param name="tutorial">要开始的教学</param>
    void BeginTutorial(Tutorial tutorial);

    /// <summary>
    /// 跳转到指定教学的特定段落。当没有指定段落时，将从第一段开始。当当前没有教学进行时，将从指定的位置开始。
    /// </summary>
    /// <param name="tutorial">要跳转的段落所属的教学</param>
    /// <param name="paragraph">要跳转的段落</param>
    void JumpToParagraph(Tutorial tutorial, TutorialParagraph? paragraph);

    /// <summary>
    /// 停止当前教学。
    /// </summary>
    void StopTutorial();

    internal void InvokeActions(IList<TutorialAction> actions);
}