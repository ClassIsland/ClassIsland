using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Abstractions.Controls.IconSources;

/// <summary>
/// 一个可以使用模板的图标源元素
/// </summary>
public abstract class TemplatedIconElement : FAIconElement
{
    private TemplatedControl _control;

    public static readonly StyledProperty<IControlTemplate> TemplateProperty = AvaloniaProperty.Register<TemplatedIconElement, IControlTemplate>(
        nameof(Template));

    public IControlTemplate Template
    {
        get => GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    /// <summary>
    /// 
    /// </summary>
    public TemplatedIconElement()
    {
        _control = new TemplatedControl()
        {
            ClipToBounds = false
        };
        _control.Bind(TemplatedControl.TemplateProperty, this.GetObservable(TemplateProperty));
        ClipToBounds = false;
        VisualChildren.Add(_control);
        LogicalChildren.Add(_control);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size constraint)
    {
        return base.MeasureOverride(constraint);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        _control.Arrange(new Rect(finalSize));

        return base.ArrangeOverride(finalSize);
    }
}