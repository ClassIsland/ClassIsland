namespace ClassIsland.Core.Enums.Tutorial;

/// <summary>
/// 代表教程动作类型。
/// </summary>
public enum TutorialActionKind
{
    /// <summary>
    /// 无
    /// </summary>
    None,
    /// <summary>
    /// 下一句
    /// </summary>
    NextSentence,
    /// <summary>
    /// 上一句
    /// </summary>
    PreviousSentence,
    /// <summary>
    /// 下一段
    /// </summary>
    NextParagraph,
    /// <summary>
    /// 上一段
    /// </summary>
    PreviousParagraph,
    /// <summary>
    /// 跳转段落
    /// </summary>
    JumpParagraph,
    /// <summary>
    /// 停止教学
    /// </summary>
    Stop,
    /// <summary>
    /// 执行行动
    /// </summary>
    InvokeActionSet,
    /// <summary>
    /// 打开指定的 Uri
    /// </summary>
    InvokeUri,
}