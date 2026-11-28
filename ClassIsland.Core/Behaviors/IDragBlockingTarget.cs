namespace ClassIsland.Core.Behaviors;

/// <summary>
/// 标记控件独占处理指针按下，列表条目的拖拽行为应忽略此控件上的按下。
/// </summary>
/// <remarks>
/// 仅应在实际处理指针输入的叶子控件上实现。
/// 若在容器上实现，其所有子控件上的按下都会被拖拽行为忽略。
/// </remarks>
public interface IDragBlockingTarget
{
}
