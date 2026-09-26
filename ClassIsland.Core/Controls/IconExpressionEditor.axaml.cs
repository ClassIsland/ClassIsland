using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.Platforms.Abstraction;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 支持字体图标和图片的图标表达式编辑器。
/// </summary>
public partial class IconExpressionEditor : UserControl
{
    /// <summary>
    /// 要编辑的图标表达式，默认使用双向绑定。
    /// </summary>
    public static readonly StyledProperty<string?> IconExpressionProperty =
        AvaloniaProperty.Register<IconExpressionEditor, string?>(nameof(IconExpression), "",
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// 获取或设置图标表达式。空字符串或 null 表示未设置图标。
    /// </summary>
    public string? IconExpression
    {
        get => GetValue(IconExpressionProperty);
        set => SetValue(IconExpressionProperty, value);
    }

    private readonly IconExpressionEditorViewModel _viewModel = new();
    private Flyout EditorFlyout => (Flyout)EditorButton.Flyout!;

    /// <summary>
    /// 创建图标表达式编辑器。
    /// </summary>
    public IconExpressionEditor()
    {
        InitializeComponent();
        EditorRoot.DataContext = FlyoutRoot.DataContext = _viewModel;
        _viewModel.ExpressionEdited += expression => SetCurrentValue(IconExpressionProperty, expression);
        _viewModel.PropertyChanged += OnEditorPropertyChanged;
        _viewModel.ApplyExpression(IconExpression);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconExpressionProperty)
            _viewModel.ApplyExpression(change.GetNewValue<string?>());
    }

    private void OnFlyoutOpened(object? sender, EventArgs e)
    {
        _viewModel.Activate();
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IconExpressionEditorViewModel.FilteredIcons))
            IconScrollViewer.Offset = default;
    }

    private void OnIconClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: IconExpressionEditorItem item })
            _viewModel.SelectIcon(item);
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Down || _viewModel.FilteredIcons.Count == 0)
            return;
        FocusIcon(0);
        e.Handled = true;
    }

    private void OnIconKeyDown(object? sender, KeyEventArgs e)
    {
        var button = (e.Source as Visual)?.GetSelfAndVisualAncestors().OfType<Button>().FirstOrDefault();
        if (button?.DataContext is not IconExpressionEditorItem item)
            return;
        var index = IconRepeater.GetElementIndex(button);
        var columns = Math.Max(1, (int)(IconRepeater.Bounds.Width / 36));
        var target = e.Key switch
        {
            Key.Left => index - 1,
            Key.Right => index + 1,
            Key.Up => index - columns,
            Key.Down => index + columns,
            Key.Home => 0,
            Key.End => _viewModel.FilteredIcons.Count - 1,
            _ => -1
        };
        if (e.Key is Key.Enter or Key.Space)
        {
            _viewModel.SelectIcon(item);
            e.Handled = true;
        }
        else if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End)
        {
            FocusIcon(Math.Clamp(target, 0, _viewModel.FilteredIcons.Count - 1));
            e.Handled = true;
        }
    }

    private void FocusIcon(int index)
    {
        var element = IconRepeater.GetOrCreateElement(index);
        // Newly realized elements need a layout pass before their scroll target is known.
        IconRepeater.UpdateLayout();
        element.BringIntoView();
        element.Focus(NavigationMethod.Directional);
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.IsBrowsing)
            return;
        var root = TopLevel.GetTopLevel(this);
        if (root == null)
        {
            _viewModel.Message = "暂时无法打开文件选择器，请直接输入图片路径。";
            return;
        }

        _viewModel.IsBrowsing = true;
        _viewModel.Message = "";
        var expressionBeforeBrowsing = IconExpression;
        EditorFlyout.Hide();
        try
        {
            PopupHelper.DisableAllPopups();
            var files = await PlatformServices.FilePickerService.OpenFilesPickerAsync(new FilePickerOpenOptions
            {
                Title = "选择图片",
                AllowMultiple = false,
                FileTypeFilter = [FilePickerFileTypes.ImageAll]
            }, root);
            // An external binding update while the picker is open takes precedence over its stale result.
            if (files.Count == 0 || IconExpression != expressionBeforeBrowsing)
                return;
            var path = files[0];
            if (PlatformServices.FilePickerService.IsBookmark(path) || !System.IO.Path.IsPathFullyQualified(path))
            {
                _viewModel.Message = "无法直接引用这张图片。请先将它保存到本地，再输入图片路径。";
                return;
            }
            _viewModel.ImagePath = path;
        }
        catch (Exception)
        {
            _viewModel.Message = "无法打开所选图片，请重新选择，或直接输入图片路径。";
        }
        finally
        {
            PopupHelper.RestoreAllPopups();
            _viewModel.IsBrowsing = false;
            if (TopLevel.GetTopLevel(this) == root && IsEffectivelyVisible)
                EditorFlyout.ShowAt(EditorButton);
        }
    }
}
