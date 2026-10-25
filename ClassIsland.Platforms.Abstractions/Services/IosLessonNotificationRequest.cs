namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 一条准备交给 iOS 系统调度、可由跨平台测试验证的课程提醒。
/// </summary>
internal sealed record IosLessonNotificationRequest(
    string Identifier,
    DateTimeOffset FireAt,
    string Title,
    string Body,
    Guid ChannelId,
    bool PlaySound,
    bool IsCatchUp = false,
    string? ChainId = null);
