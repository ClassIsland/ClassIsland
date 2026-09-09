using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Primitives;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Models;

namespace ClassIsland.Controls.ScheduleWeekEdit;

public sealed class ScheduleWeekBlock : TemplatedControl
{
    public static readonly StyledProperty<string> SubjectNameProperty =
        AvaloniaProperty.Register<ScheduleWeekBlock, string>(nameof(SubjectName), "");
    public static readonly StyledProperty<string> TimeTextProperty =
        AvaloniaProperty.Register<ScheduleWeekBlock, string>(nameof(TimeText), "");
    public static readonly StyledProperty<bool> ShowHandlesProperty =
        AvaloniaProperty.Register<ScheduleWeekBlock, bool>(nameof(ShowHandles));
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<ScheduleWeekBlock, bool>(nameof(IsSelected));
    public static readonly StyledProperty<bool> IsCompactProperty =
        AvaloniaProperty.Register<ScheduleWeekBlock, bool>(nameof(IsCompact));

    public string SubjectName { get => GetValue(SubjectNameProperty); set => SetValue(SubjectNameProperty, value); }
    public string TimeText { get => GetValue(TimeTextProperty); set => SetValue(TimeTextProperty, value); }
    public bool ShowHandles { get => GetValue(ShowHandlesProperty); set => SetValue(ShowHandlesProperty, value); }
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public bool IsCompact { get => GetValue(IsCompactProperty); set => SetValue(IsCompactProperty, value); }
    public required ScheduleWeekOccurrence Occurrence { get; set; }

    internal void UpdateSelection(bool selected, bool isReadonly, bool animate)
    {
        // Set the initial opacity before attaching a new block; only subsequent selection changes transition.
        if (animate && IThemeService.AnimationLevel >= 1 && !IThemeService.IsTransientDisabled)
            Transitions ??= new Transitions
            {
                new DoubleTransition
                {
                    Property = OpacityProperty,
                    Duration = TimeSpan.FromMilliseconds(150),
                    Easing = new SplineEasing(0.2, 0, 0, 1)
                }
            };
        else
            Transitions = null;
        IsSelected = selected;
        ShowHandles = selected && !isReadonly;
        Opacity = selected ? 1 : 0.55;
    }
}
