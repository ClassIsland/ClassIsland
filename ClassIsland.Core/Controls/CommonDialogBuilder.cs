using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClassIsland.Core.Controls.CommonDialog;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Core.Controls;

public class CommonDialogBuilder
{
    public CommonDialog.CommonDialog Dialog { get; } = new();

    public CommonDialogBuilder SetCaption(string caption)
    {
        Dialog.Title = caption;
        return this;
    }

    public CommonDialogBuilder SetContent(string content)
    {
        Dialog.DialogContent = content;
        return this;
    }

    public CommonDialogBuilder SetIconVisual(Visual? visual)
    {
        Dialog.DialogIcon = visual;
        return this;
    }

    public CommonDialogBuilder SetBitmapIcon(BitmapSource source, double width = 64, double height = 64)
    {
        Dialog.DialogIcon = new Image()
        {
            Source = source,
            Width = width,
            Height = height
        };
        return this;
    }

    public CommonDialogBuilder SetBitmapIcon(Uri uri, double width = 64, double height = 64) =>
        SetBitmapIcon(new BitmapImage(uri), width, height);

    public CommonDialogBuilder SetPackIcon(PackIconKind kind, double width = 64, double height = 64)
    {
        Dialog.DialogIcon = new PackIcon()
        {
            Kind = kind,
            Width = width,
            Height = height
        };
        return this;
    }

    public CommonDialogBuilder AddAction(DialogAction action)
    {
        Dialog.Actions.Add(action);
        return this;
    }

    public CommonDialogBuilder HasInput(bool b)
    {
        Dialog.HasInput = b;
        return this;
    }

    public CommonDialogBuilder AddAction(string name, PackIconKind icon, bool isPrimary=false)
    {
        return AddAction(new DialogAction()
        {
            Name = name,
            PackIconKind = icon,
            IsPrimary = isPrimary
        });
    }

    public CommonDialogBuilder AddConfirmAction() => AddAction("确定", PackIconKind.Check);
    public CommonDialogBuilder AddYesAction() => AddAction("是", PackIconKind.Check);
    public CommonDialogBuilder AddNoAction() => AddAction("否", PackIconKind.Close);
    public CommonDialogBuilder AddCancelAction() => AddAction("取消", PackIconKind.Cancel);

    public CommonDialog.CommonDialog Build() => Dialog;

    public int ShowDialog(Window? owner=null)
    {
        Dialog.Owner = owner;
        Dialog.ShowDialog();
        return Dialog.ExecutedActionIndex;
    }

    public int ShowDialog(out string inputResult, Window? owner = null)
    {
        var r = ShowDialog(owner);
        inputResult = Dialog.InputResult;
        return r;
    }
}