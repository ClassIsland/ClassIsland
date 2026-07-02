using System.ComponentModel;
using Avalonia.Controls;

namespace ClassIsland.Core.Models.UI;

public class ViewClosingEventArgs : CancelEventArgs
{
    internal ViewClosingEventArgs(WindowCloseReason reason, bool isProgrammatic, bool isCancelable)
    {
        ViewHostCloseReason = reason;
        IsProgrammatic = isProgrammatic;
        IsCancelable = isCancelable;
    }

    /// <summary>
    /// Gets a value that indicates why the window is being closed.
    /// </summary>
    public WindowCloseReason ViewHostCloseReason { get; }

    /// <summary>
    /// Gets a value indicating whether the window is being closed programmatically.
    /// </summary>
    public bool IsProgrammatic { get; }

    /// <summary>
    /// 是否可以取消关闭流程
    /// </summary>
    public bool IsCancelable { get; }
}
