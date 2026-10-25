namespace ClassIsland.Platforms.Abstraction.Models.LiveActivities;

/// <summary>
/// 灵动岛展开态使用的轻量组件摘要；原生扩展只负责渲染，不解析 Avalonia 组件配置。
/// </summary>
public sealed record LessonLiveActivityComponent(
    string Kind,
    string Text = "",
    string SecondaryText = "",
    string SymbolName = "",
    string ExtraText = "",
    double? TargetTime = null,
    bool ShowProgress = false);
