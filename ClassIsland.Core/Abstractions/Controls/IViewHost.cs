namespace ClassIsland.Core.Abstractions.Controls;

/// <summary>
/// 视图宿主。
/// </summary>
public interface IViewHost
{
    /// <summary>
    /// 所有者视图宿主
    /// </summary>
    IViewHost? Owner { get; }

    /// <summary>
    /// 将视图激活到视图宿主中。
    /// </summary>
    /// <param name="view">激活的视图</param>
    bool ActivateView(ViewBase view);

    /// <summary>
    /// 取消激活视图。
    /// </summary>
    /// <param name="view">要取消激活的视图</param>
    bool DeactivateView(ViewBase view);

    /// <summary>
    /// 显示视图宿主
    /// </summary>。
    void Show();

    /// <summary>
    /// 显示视图宿主，并将另一个试图宿主作为该试图宿主的父级。
    /// </summary>
    /// <param name="owner">所有者视图宿主</param>
    void Show(IViewHost owner);

    /// <summary>
    /// 隐藏视图宿主。
    /// </summary>
    void Hide();

    /// <summary>
    /// 显示指定的视图。
    /// </summary>
    /// <remarks>视图必须已经激活，才能在试图宿主上显示。</remarks>
    /// <param name="view">要显示的视图</param>
    Task ShowView(ViewBase view);

    /// <summary>
    /// 隐藏指定的视图
    /// </summary>
    /// <param name="view">要隐藏的视图</param>
    Task<bool> HideView(ViewBase view);
}