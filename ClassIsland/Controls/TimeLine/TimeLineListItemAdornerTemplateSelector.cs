using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Metadata;

namespace ClassIsland.Controls.TimeLine;

public class TimeLineListItemAdornerTemplateSelector : AvaloniaObject, IControlTemplate
{
    public const string ExpandingTimePointAdornerKey = "ExpandingTimePointAdorner";
    public const string LineTimePointAdornerKey = "LineTimePointAdorner";
    
    [Content]
    public Dictionary<string, ControlTemplate> Templates { get; set; } = new();

    private int _timeType;

    public static readonly DirectProperty<TimeLineListItemAdornerTemplateSelector, int> TimeTypeProperty = AvaloniaProperty.RegisterDirect<TimeLineListItemAdornerTemplateSelector, int>(
        nameof(TimeType), o => o.TimeType, (o, v) => o.TimeType = v);

    public int TimeType
    {
        get => _timeType;
        set => SetAndRaise(TimeTypeProperty, ref _timeType, value);
    }
    
    public TemplateResult<Control>? Build(TemplatedControl param)
    {
        return TimeType switch
        {
            0 or 1 => Templates.GetValueOrDefault(ExpandingTimePointAdornerKey)?.Build(param),
            2 or 3 => Templates.GetValueOrDefault(LineTimePointAdornerKey)?.Build(param),
            _ => null
        };
    }
}