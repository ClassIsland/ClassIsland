using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace ClassIsland.Core.Controls;

/// <summary>
/// 禁止自身滚动并将鼠标滚轮事件传递给父容器的ListBox，
/// 适用于嵌套在ScrollViewer中的场景。
/// </summary>
public class NonScrollingListBox : ListBox
{
    /// <inheritdoc />
    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        // 如果事件已被处理则直接返回
        if (e.Handled) return;

        // 标记事件已处理（阻止ListBox自身滚动）
        e.Handled = true;

        // 创建新事件冒泡给父元素
        var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = MouseWheelEvent,
            Source = this
        };

        // 触发父元素事件
        if (Parent is UIElement parent)
        {
            parent.RaiseEvent(eventArg);
        }
    }

}