namespace ClassIsland.iOS.Services.Notifications;

/// <summary>
/// 一条准备交给 iOS 系统调度的课程提醒。
/// </summary>
internal sealed record IosLessonNotificationRequest(
    string Identifier,
    DateTimeOffset FireAt,
    string Title,
    string Body,
    bool PlaySound,
    bool IsCatchUp = false);
