using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Models.ComponentSettings;
using ReactiveUI;

namespace ClassIsland.Controls.Components;

/// <summary>
/// TestComponent.xaml 的交互逻辑
/// </summary>
[PseudoClasses(":custom-font-color")]
[ComponentInfo("EE8F66BD-C423-4E7C-AB46-AA9976B00E08", "文本", "\uf323", "显示自定义文本。")]
public partial class TextComponent : ComponentBase<TextComponentSettings>
{
    private IDisposable? _useCustomFontColorObserver;
    
    public TextComponent()
    {
        InitializeComponent();
    }

    private void Visual_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _useCustomFontColorObserver ??= Settings.ObservableForProperty(x => x.UseCustomFontColor)
            .Subscribe(_ => PseudoClasses.Set(":custom-font-color", Settings.UseCustomFontColor));
        PseudoClasses.Set(":custom-font-color", Settings.UseCustomFontColor);
    }

    private void Visual_OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _useCustomFontColorObserver?.Dispose();
        _useCustomFontColorObserver = null;
    }
}
