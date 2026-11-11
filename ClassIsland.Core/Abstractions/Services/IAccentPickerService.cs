using System.ComponentModel;
using Avalonia.Media;

namespace ClassIsland.Core.Abstractions.Services;

/// <summary>提取并应用当前设置所指定的主题色。</summary>
public interface IAccentPickerService : INotifyPropertyChanged
{
    /// <summary>当前平台支持壁纸取色。</summary>
    bool SupportsWallpaper { get; }

    /// <summary>当前平台支持屏幕取色。</summary>
    bool SupportsScreen { get; }

    /// <summary>提取结果可提供图像预览。</summary>
    bool SupportsPreview { get; }

    /// <summary>正在刷新主题色。</summary>
    bool IsWorking { get; }

    /// <summary>当前来源及目标最近一次成功的结果。</summary>
    AccentPickResult? LastResult { get; }

    /// <summary>刷新当前主题色。取消令牌可取消调用方的等待，共享刷新可继续。</summary>
    Task<AccentPickResult> RefreshAsync(CancellationToken cancellationToken = default);
}

/// <summary>一次主题色刷新及其可选的 PNG 预览。</summary>
public sealed record AccentPickResult(AccentPickStatus Status, Color? Color = null,
    ReadOnlyMemory<byte> PreviewPng = default);

/// <summary>主题色刷新结果。</summary>
public enum AccentPickStatus
{
    /// <summary>已提取并应用主题色。</summary>
    Success,
    /// <summary>当前来源不支持、未就绪或已切换。</summary>
    Unavailable,
    /// <summary>无法取得、处理或保存当前来源的颜色。</summary>
    Failed
}
