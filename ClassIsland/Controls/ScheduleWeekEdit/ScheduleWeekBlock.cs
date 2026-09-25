using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Models;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Controls.ScheduleWeekEdit;

public sealed class ScheduleWeekBlock : TemplatedControl
{
    public static readonly StyledProperty<string> SubjectNameProperty =
        AvaloniaProperty.Register<ScheduleWeekBlock, string>(nameof(SubjectName), "");
    public static readonly StyledProperty<IBrush> SubjectBrushProperty =
        AvaloniaProperty.Register<ScheduleWeekBlock, IBrush>(nameof(SubjectBrush), Brushes.Gray);
    public static readonly StyledProperty<string> TimeTextProperty =
        AvaloniaProperty.Register<ScheduleWeekBlock, string>(nameof(TimeText), "");
    public static readonly StyledProperty<bool> ShowHandlesProperty =
        AvaloniaProperty.Register<ScheduleWeekBlock, bool>(nameof(ShowHandles));
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<ScheduleWeekBlock, bool>(nameof(IsSelected));
    public static readonly StyledProperty<bool> IsCompactProperty =
        AvaloniaProperty.Register<ScheduleWeekBlock, bool>(nameof(IsCompact));
    public static readonly StyledProperty<bool> ShowTimeTextProperty =
        AvaloniaProperty.Register<ScheduleWeekBlock, bool>(nameof(ShowTimeText), true);

    public string SubjectName { get => GetValue(SubjectNameProperty); set => SetValue(SubjectNameProperty, value); }
    public IBrush SubjectBrush { get => GetValue(SubjectBrushProperty); set => SetValue(SubjectBrushProperty, value); }
    public string TimeText { get => GetValue(TimeTextProperty); set => SetValue(TimeTextProperty, value); }
    public bool ShowHandles { get => GetValue(ShowHandlesProperty); set => SetValue(ShowHandlesProperty, value); }
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public bool IsCompact { get => GetValue(IsCompactProperty); set => SetValue(IsCompactProperty, value); }
    public bool ShowTimeText { get => GetValue(ShowTimeTextProperty); set => SetValue(ShowTimeTextProperty, value); }
    public required ScheduleWeekOccurrence Occurrence { get; set; }
    private StackPanel? _labels;
    private TextBlock? _subjectLabel;
    private string? _subjectColorHex;
    private string? _subjectIconExpression;
    private FAIconSource? _subjectIconSource;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _labels = e.NameScope.Find<StackPanel>("Labels");
        _subjectLabel = e.NameScope.Find<TextBlock>("PART_SubjectLabel");
        UpdateLabelsScale();
        UpdateSubjectTitle();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SubjectNameProperty)
            UpdateSubjectTitle();
        else if (change.Property == BoundsProperty || change.Property == IsCompactProperty)
            UpdateLabelsScale();
    }

    private void UpdateLabelsScale()
    {
        if (_labels?.RenderTransform is not ScaleTransform transform || Bounds.Width <= 0) return;
        var availableWidth = Math.Max(0, Bounds.Width - _labels.Margin.Left - _labels.Margin.Right);
        var scale = Math.Min(1, availableWidth / _labels.MinWidth);
        transform.ScaleX = scale;
        transform.ScaleY = scale;
    }

    internal void UpdateSubjectAppearance(ScheduleWeekOccurrence item)
    {
        if (_subjectColorHex != item.SubjectColorHex)
        {
            _subjectColorHex = item.SubjectColorHex;
            SubjectBrush = Color.TryParse(item.SubjectColorHex, out var color)
                ? new SolidColorBrush(color) : Brushes.Gray;
        }

        var iconChanged = _subjectIconExpression != item.SubjectIconExpression;
        if (iconChanged)
        {
            _subjectIconExpression = item.SubjectIconExpression;
            _subjectIconSource = string.IsNullOrWhiteSpace(item.SubjectIconExpression)
                ? null : IconExpressionHelper.TryParseOrNull(item.SubjectIconExpression);
        }
        var nameChanged = SubjectName != item.SubjectName;
        SubjectName = item.SubjectName;
        if (iconChanged && !nameChanged)
            UpdateSubjectTitle();
    }

    private void UpdateSubjectTitle()
    {
        if (_subjectLabel == null) return;
        var inlines = _subjectLabel.Inlines!;
        inlines.Clear();
        if (_subjectIconSource != null)
        {
            var icon = new FAIconSourceElement
            {
                IconSource = _subjectIconSource,
                Width = 14,
                Height = 14,
                IsHitTestVisible = false
            };
            TextElement.SetFontSize(icon, 14);
            icon.Classes.Add("repair-fontsize");
            inlines.Add(icon);
            // Keep the icon with the start of the title without changing the text baseline.
            inlines.Add(new Run("\u2060\u202F" + SubjectName));
            return;
        }
        inlines.Add(new Run(SubjectName));
    }

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
