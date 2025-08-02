namespace ClassIsland.Platforms.Abstraction.Models;

/// <summary>
/// 代表桌面通知的内容
/// </summary>
public class DesktopToastContent
{
    /// <summary>
    /// 通知标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 通知正文
    /// </summary>
    public string Body { get; set; } = "";

    /// <summary>
    /// 通知头图
    /// </summary>
    public Uri? HeroImageUri { get; set; }
    
    /// <summary>
    /// 通知内容图片
    /// </summary>
    public Uri? InlineImageUri { get; set; }
    
    /// <summary>
    /// 通知 Logo 图片 Uri
    /// </summary>
    public Uri? LogoImageUri { get; set; }

    /// <summary>
    /// 在这条通知被激活后触发
    /// </summary>
    public EventHandler? Activated;

    /// <summary>
    /// 通知的按钮
    /// </summary>
    public Dictionary<string, Action> Buttons { get; set; } = new Dictionary<string, Action>();
}