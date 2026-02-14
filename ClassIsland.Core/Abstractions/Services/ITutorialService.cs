using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
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
    /// 当前是否有教程正在运行
    /// </summary>
    bool IsTutorialRunning { get; }
    
    /// <summary>
    /// 当前附加到的 <see cref="TopLevel"/>
    /// </summary>
    public TopLevel? AttachedToplevel { get; }

    /// <summary>
    /// 开始指定的教学。
    /// </summary>
    /// <remarks>
    /// 仅在当前没有教学进行时生效。当当前有教学进行时，调用此方法不会有任何作用。
    /// </remarks>
    /// <param name="tutorial">要开始的教学</param>
    void BeginTutorial(Tutorial tutorial);

    /// <summary>
    /// 开始指定的教学。
    /// </summary>
    /// <remarks>
    /// 仅在当前没有教学进行时生效。当当前有教学进行时，调用此方法不会有任何作用。
    /// </remarks>
    /// <param name="path">要开始的教学的路径</param>
    /// <param name="requiresNotCompleted">仅在教程未完成时才运行</param>
    void BeginTutorial(string path, bool requiresNotCompleted = false);

    /// <summary>
    /// 跳转到指定教学的特定段落。当没有指定段落时，将从第一段开始。当当前没有教学进行时，将从指定的位置开始。
    /// </summary>
    /// <param name="tutorial">要跳转的段落所属的教学</param>
    /// <param name="paragraph">要跳转的段落</param>
    void JumpToParagraph(Tutorial tutorial, TutorialParagraph? paragraph);

    /// <summary>
    /// 手动将当前教程向前推动一个语句。仅在当前语句设置了 <see cref="TutorialSentence.WaitForNextCommand"/> 为 true 时生效。
    /// </summary>
    /// <param name="paragraphPath">限定生效的教程段落路径。设置此参数后，仅在教程段落路径为这个值时此方法才起作用</param>
    void PushToNextSentence(string? paragraphPath = null);

    /// <summary>
    /// 停止当前教学。
    /// </summary>
    void StopTutorial();

    /// <summary>
    /// 当教程状态变化时触发。
    /// </summary>
    event EventHandler? TutorialStateChanged;

    internal void InvokeActions(IList<TutorialAction> actions);
}