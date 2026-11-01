namespace ClassIsland.Core.Models.UI;

/// <summary>
/// 视图宿主抽屉的显示选项。
/// </summary>
/// <remarks>显式赋值的显示选项优先于当前页面的附加属性，未赋值时使用页面配置或默认值。</remarks>
public sealed class DrawerOptions
{
    private double? _contentHeightRatio;
    private double? _contentHeight;
    private double? _maxWidth;
    private bool? _isFullScreen;
    private bool? _allowCollapse;
    private bool? _smokeBackground;

    internal bool IsContentHeightRatioSet => _contentHeightRatio.HasValue;
    internal bool IsContentHeightSet { get; private set; }
    internal bool IsMaxWidthSet => _maxWidth.HasValue;
    internal bool IsFullScreenSet => _isFullScreen.HasValue;
    internal bool IsAllowCollapseSet => _allowCollapse.HasValue;
    internal bool IsSmokeBackgroundSet => _smokeBackground.HasValue;

    /// <summary>
    /// 抽屉的最大宽度，默认不限制。
    /// </summary>
    public double MaxWidth
    {
        get => _maxWidth ?? double.PositiveInfinity;
        init => _maxWidth = value;
    }

    /// <summary>
    /// 抽屉展开高度占宿主高度的比例。默认值 <c>0.5</c>。
    /// </summary>
    public double ContentHeightRatio
    {
        get => _contentHeightRatio ?? 0.5;
        init => _contentHeightRatio = value;
    }

    /// <summary>
    /// 抽屉展开后的绝对高度。设置后优先于 <see cref="ContentHeightRatio"/>。
    /// </summary>
    public double? ContentHeight
    {
        get => _contentHeight;
        init
        {
            _contentHeight = value;
            IsContentHeightSet = true;
        }
    }

    /// <summary>
    /// 是否以全屏高度打开抽屉。
    /// </summary>
    public bool IsFullScreen
    {
        get => _isFullScreen ?? false;
        init => _isFullScreen = value;
    }

    /// <summary>
    /// 是否允许收起抽屉。
    /// </summary>
    public bool AllowCollapse
    {
        get => _allowCollapse ?? false;
        init => _allowCollapse = value;
    }

    /// <summary>
    /// 是否显示遮罩。
    /// </summary>
    public bool SmokeBackground
    {
        get => _smokeBackground ?? true;
        init => _smokeBackground = value;
    }

    /// <summary>
    /// 固定标题。为空时使用当前页面标题。
    /// </summary>
    public object? Title { get; init; }

    /// <summary>
    /// 固定标题栏内容。为空时使用当前页面的顶部命令栏。
    /// </summary>
    public object? TitleBarContent { get; init; }
}
