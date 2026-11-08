namespace ClassIsland.Models.SystemIntegration;

internal sealed record SystemScheduleSnapshot(
    long GeneratedAt,
    long ValidUntil,
    bool NotificationsEnabled,
    SystemScheduleLesson[] Lessons,
    SystemScheduleClassPlan[] ClassPlans);

internal sealed record SystemScheduleLesson(
    string Id,
    string Subject,
    string Location,
    long StartAt,
    long EndAt,
    string Url);

internal sealed record SystemScheduleClassPlan(string Id, string Name, string Url);
