using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 可为空的内容控件。
/// </summary>
[PseudoClasses(":empty", ":default-empty-content")]
public class Emptiable : TemplatedControl
{
    public static readonly StyledProperty<object?> DataProperty = AvaloniaProperty.Register<Emptiable, object?>(
        nameof(Data));

    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public static readonly StyledProperty<bool> IsDirectContentModeProperty = AvaloniaProperty.Register<Emptiable, bool>(
        nameof(IsDirectContentMode));

    public bool IsDirectContentMode
    {
        get => GetValue(IsDirectContentModeProperty);
        set => SetValue(IsDirectContentModeProperty, value);
    }
    
    public static readonly StyledProperty<object?> ContentProperty = AvaloniaProperty.Register<Emptiable, object?>(
        nameof(Content));

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate> ContentTemplateProperty = AvaloniaProperty.Register<Emptiable, IDataTemplate>(
        nameof(ContentTemplate));

    public IDataTemplate ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public static readonly StyledProperty<object?> EmptyContentProperty = AvaloniaProperty.Register<Emptiable, object?>(
        nameof(EmptyContent));

    public object? EmptyContent
    {
        get => GetValue(EmptyContentProperty);
        set => SetValue(EmptyContentProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate> EmptyContentTemplateProperty = AvaloniaProperty.Register<Emptiable, IDataTemplate>(
        nameof(EmptyContentTemplate));

    public IDataTemplate EmptyContentTemplate
    {
        get => GetValue(EmptyContentTemplateProperty);
        set => SetValue(EmptyContentTemplateProperty, value);
    }

    private void UpdateContentEmptyState()
    {
        PseudoClasses.Set(":empty", IsDirectContentMode ? Data == null : Content == null);
    }

    public Emptiable()
    {
        this.GetObservable(ContentProperty).Subscribe(_ => UpdateContentEmptyState());
        this.GetObservable(DataProperty).Subscribe(_ => UpdateContentEmptyState());
        this.GetObservable(IsDirectContentModeProperty).Subscribe(_ => UpdateContentEmptyState());
        this.GetObservable(EmptyContentProperty)
            .Subscribe(_ => PseudoClasses.Set(":default-empty-content", EmptyContent == null));
    }
}