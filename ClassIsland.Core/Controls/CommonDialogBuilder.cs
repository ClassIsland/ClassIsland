using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Enums;
using ClassIsland.Shared;
using Material.Icons;
using Material.Icons.Avalonia;

namespace ClassIsland.Core.Controls;

/// <summary>
/// <see cref="CommonDialog"/> 构筑类
/// </summary>
public class CommonDialogBuilder
{
    /// <summary>
    /// <see cref="CommonDialog"/> 实例
    /// </summary>
    public CommonDialog.CommonDialog Dialog { get; } = new();

    /// <summary>
    /// 设置对话框的标题。
    /// </summary>
    /// <param name="caption">要设置的标题</param>
    /// <returns>原来的 <see cref="CommonDialogBuilder"/> 对象</returns>
    public CommonDialogBuilder SetCaption(string caption)
    {
        Dialog.Title = caption;
        return this;
    }

    /// <summary>
    /// 设置对话框的内容。
    /// </summary>
    /// <param name="content">要设置的内容</param>
    /// <returns>原来的 <see cref="CommonDialogBuilder"/> 对象</returns>
    public CommonDialogBuilder SetContent(string content)
    {
        Dialog.DialogContent = content;
        return this;
    }

    /// <summary>
    /// 设置自定义图标 <see cref="Visual"/> 对象。
    /// </summary>
    /// <param name="visual">自定义的 <see cref="Visual"/> 对象</param>
    /// <returns>原来的 <see cref="CommonDialogBuilder"/> 对象</returns>
    public CommonDialogBuilder SetIconVisual(Visual? visual)
    {
        Dialog.DialogIcon = visual;
        return this;
    }

    /// <summary>
    /// 设置自定义位图图标。
    /// </summary>
    /// <param name="source">自定义位图 <see cref="BitmapSource"/></param>
    /// <param name="width">位图宽度（px），默认为 64</param>
    /// <param name="height">位图高度（px），默认为 64</param>
    /// <returns>原来的 <see cref="CommonDialogBuilder"/> 对象</returns>
    public CommonDialogBuilder SetBitmapIcon(Bitmap source, double width = 64, double height = 64)
    {
        Dialog.DialogIcon = new Image()
        {
            Source = source,
            Width = width,
            Height = height
        };
        return this;
    }

    /// <summary>
    /// 设置自定义位图图标。
    /// </summary>
    /// <param name="uri">自定义位图的 Uri</param>
    /// <param name="width">位图宽度（px），默认为 64</param>
    /// <param name="height">位图高度（px），默认为 64</param>
    /// <remarks>
    /// 如果您想设置一个内置的表情包图标，建议使用 <see cref="SetIconKind"/> 方法。此方法可以在【禁用彩蛋】策略启用时自动切换到对应的 <see cref="PackIcon"/> 图标。
    /// </remarks>
    /// <returns>原来的 <see cref="CommonDialogBuilder"/> 对象</returns>
    public CommonDialogBuilder SetBitmapIcon(Uri uri, double width = 64, double height = 64) =>
        SetBitmapIcon(new Bitmap(AssetLoader.Open(uri)), width, height);

    /// <summary>
    /// 设置图标类型。
    /// </summary>
    /// <param name="kind">图标类型</param>
    /// <returns>原来的 <see cref="CommonDialogBuilder"/> 对象</returns>
    public CommonDialogBuilder SetIconKind(CommonDialogIconKind kind)
    {
        var managementService = IAppHost.TryGetService<IManagementService>();
        return managementService?.Policy.DisableEasterEggs == true
            ? kind switch
            {
                CommonDialogIconKind.Information => SetPackIcon(MaterialIconKind.InfoCircle),
                CommonDialogIconKind.Hint => SetPackIcon(MaterialIconKind.WarningCircle),
                CommonDialogIconKind.Forbidden => SetPackIcon(MaterialIconKind.AlertOctagon),
                CommonDialogIconKind.Error => SetPackIcon(MaterialIconKind.CloseCircle),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            }
            : kind switch
            {
                CommonDialogIconKind.Information => SetBitmapIcon(new Uri("avares://ClassIsland/Assets/HoYoStickers/帕姆_点赞.png",
                    UriKind.RelativeOrAbsolute)),
                CommonDialogIconKind.Hint => SetBitmapIcon(new Uri("avares://ClassIsland/Assets/HoYoStickers/帕姆_注意.png",
                    UriKind.RelativeOrAbsolute)),
                CommonDialogIconKind.Forbidden => SetBitmapIcon(new Uri("avares://ClassIsland/Assets/HoYoStickers/帕姆_不可以.png",
                    UriKind.RelativeOrAbsolute)),
                CommonDialogIconKind.Error => SetBitmapIcon(new Uri("avares://ClassIsland/Assets/HoYoStickers/帕姆_哭哭.png",
                    UriKind.RelativeOrAbsolute)),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
    }

    /// <summary>
    /// 设置自定义 <see cref="PackIcon"/> 图标。
    /// </summary>
    /// <param name="kind">自定义图标的图表类型</param>
    /// <param name="width">图标宽度（px），默认为 64</param>
    /// <param name="height">图标高度（px），默认为 64</param>
    /// <returns>原来的 <see cref="CommonDialogBuilder"/> 对象</returns>
    public CommonDialogBuilder SetPackIcon(MaterialIconKind kind, double width = 64, double height = 64)
    {
        Dialog.DialogIcon = new MaterialIcon()
        {
            Kind = kind,
            Width = width,
            Height = height
        };
        return this;
    }

    /// <summary>
    /// 添加一个操作按钮。
    /// </summary>
    /// <param name="action">操作信息</param>
    /// <returns>原来的 <see cref="CommonDialogBuilder"/> 对象</returns>
    public CommonDialogBuilder AddAction(DialogAction action)
    {
        Dialog.Actions.Add(action);
        return this;
    }

    /// <summary>
    /// 是否显示输入框。
    /// </summary>
    /// <param name="b">是否显示输入框</param>
    /// <returns>原来的 <see cref="CommonDialogBuilder"/> 对象</returns>
    public CommonDialogBuilder HasInput(bool b)
    {
        Dialog.HasInput = b;
        return this;
    }

    /// <summary>
    /// 添加一个操作按钮。
    /// </summary>
    /// <param name="name">操作名称</param>
    /// <param name="icon">操作的图标包类型</param>
    /// <param name="isPrimary">操作是否是主要操作，按下 Enter 时将默认选择</param>
    /// <returns>原来的 <see cref="CommonDialogBuilder"/> 对象</returns>
    public CommonDialogBuilder AddAction(string name, MaterialIconKind icon, bool isPrimary=false)
    {
        return AddAction(new DialogAction()
        {
            Name = name,
            MaterialIconKind = icon,
            IsPrimary = isPrimary
        });
    }

    /// <summary>
    /// 添加一个“确定”操作按钮。
    /// </summary>
    /// <returns>原来的 <see cref="CommonDialogBuilder"/> 对象</returns>
    public CommonDialogBuilder AddConfirmAction() => AddAction("确定", MaterialIconKind.Check);

    /// <summary>
    /// 添加一个“是”操作按钮。
    /// </summary>
    /// <returns>原来的 <see cref="CommonDialogBuilder"/> 对象</returns>
    public CommonDialogBuilder AddYesAction() => AddAction("是", MaterialIconKind.Check);

    /// <summary>
    /// 添加一个“否”操作按钮。
    /// </summary>
    /// <returns>原来的 <see cref="CommonDialogBuilder"/> 对象</returns>
    public CommonDialogBuilder AddNoAction() => AddAction("否", MaterialIconKind.Close);

    /// <summary>
    /// 添加一个“取消”操作按钮。
    /// </summary>
    /// <returns>原来的 <see cref="CommonDialogBuilder"/> 对象</returns>
    public CommonDialogBuilder AddCancelAction() => AddAction("取消", MaterialIconKind.Cancel);

    /// <summary>
    /// 获得构建的 <see cref="CommonDialog"/> 对象。
    /// </summary>
    /// <returns>构建的 <see cref="CommonDialog"/> 对象</returns>
    public CommonDialog.CommonDialog Build() => Dialog;

    /// <summary>
    /// 显示构建的对话框。
    /// </summary>
    /// <param name="owner">对话框所有者</param>
    /// <returns>对话框选择的返回值</returns>
    public async Task<int> ShowDialog(Window? owner=null)
    {
        var window = owner ?? AppBase.Current.MainWindow;
        if (window == null)
        {
            var cancel = new CancellationTokenSource();

            void OnDialogOnClosed(object? s, EventArgs e)
            {
                cancel.Cancel();
                Dialog.Closed -= OnDialogOnClosed;
            }
            Dialog.Closed += OnDialogOnClosed;
            Dialog.Show();

            if (!cancel.Token.IsCancellationRequested)
            {
                await Task.Run(() => cancel.Token.WaitHandle.WaitOne(), cancel.Token);
            }
        }
        else
        {
            await Dialog.ShowDialog(window);
        }
        return Dialog.ExecutedActionIndex;
    }

    /// <summary>
    /// 显示构建的对话框。
    /// </summary>
    /// <param name="owner">对话框所有者</param>
    /// <returns>对话框选择的返回值</returns>
    public async Task<(int, string)> ShowDialogWithOutput(Window? owner = null)
    {
        var r = await ShowDialog(owner);
        return (r, Dialog.InputResult);
    }
}