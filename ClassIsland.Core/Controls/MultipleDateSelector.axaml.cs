using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;
using ClassIsland.Core.Models.UI;
using Calendar = Avalonia.Controls.Calendar;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 使用月历选择多个日期，并在单行预览中显示所选日期。
/// </summary>
public partial class MultipleDateSelector : UserControl
{
    /// <summary>
    /// 已选择的日期，默认使用双向绑定。
    /// </summary>
    public static readonly StyledProperty<ObservableCollection<DateOnly>?> SelectedDatesProperty =
        AvaloniaProperty.Register<MultipleDateSelector, ObservableCollection<DateOnly>?>(nameof(SelectedDates),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// 未选择日期时的提示文字。
    /// </summary>
    public static readonly StyledProperty<string> PlaceholderTextProperty =
        AvaloniaProperty.Register<MultipleDateSelector, string>(nameof(PlaceholderText), "选择日期");

    /// <summary>
    /// 获取或设置已选择的日期。null 按空集合显示，首次选择时创建集合。
    /// 控件直接修改集合，不调整外部集合的顺序。
    /// </summary>
    public ObservableCollection<DateOnly>? SelectedDates
    {
        get => GetValue(SelectedDatesProperty);
        set => SetValue(SelectedDatesProperty, value);
    }

    /// <summary>
    /// 获取或设置未选择日期时的提示文字。
    /// </summary>
    public string PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    private readonly MultipleDateSelectorViewModel _viewModel = new();
    private ObservableCollection<DateOnly>? _observedDates;
    private bool _initialized;
    private bool _detached;
    private bool _updatingDates;

    private Flyout CalendarFlyout => (Flyout)PreviewButton.Flyout!;

    /// <summary>
    /// 创建日期多选控件。
    /// </summary>
    public MultipleDateSelector()
    {
        InitializeComponent();
        PreviewRoot.DataContext = MonthCalendar.DataContext = _viewModel;
        MonthCalendar.AddHandler(Button.ClickEvent, OnCalendarClick, RoutingStrategies.Bubble);
        MonthCalendar.AddHandler(KeyDownEvent, OnCalendarKeyDown, RoutingStrategies.Tunnel);
        PreviewButton.AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        PreviewText.SizeChanged += (_, _) => UpdatePreview();
        PreviewText.PropertyChanged += (_, change) =>
        {
            if (change.Property == TextBlock.FontFamilyProperty || change.Property == TextBlock.FontSizeProperty ||
                change.Property == TextBlock.FontWeightProperty || change.Property == TextBlock.FontStyleProperty ||
                change.Property == TextBlock.FontStretchProperty || change.Property == TextBlock.FontFeaturesProperty ||
                change.Property == TextBlock.LetterSpacingProperty || change.Property == FlowDirectionProperty)
                UpdatePreview();
        };
        _initialized = true;
        SetCurrentValue(SelectedDatesProperty, new ObservableCollection<DateOnly>());
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (!_initialized)
            return;
        if (change.Property == SelectedDatesProperty)
            ObserveDates();
        else if (change.Property == PlaceholderTextProperty)
            UpdateDates();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _detached = false;
        ObserveDates();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _detached = true;
        CalendarFlyout.Hide();
        UnsubscribeDates();
        base.OnDetachedFromVisualTree(e);
    }

    private void UnsubscribeDates()
    {
        if (_observedDates != null)
            _observedDates.CollectionChanged -= OnDatesChanged;
        _observedDates = null;
    }

    private void ObserveDates()
    {
        UnsubscribeDates();
        if (!_detached)
        {
            _observedDates = SelectedDates;
            if (_observedDates != null)
                _observedDates.CollectionChanged += OnDatesChanged;
        }
        UpdateDates();
    }

    private void OnDatesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_updatingDates)
            UpdateDates();
    }

    private void UpdateDates()
    {
        _viewModel.UpdateDates(SelectedDates, PlaceholderText);
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (!_initialized)
            return;

        // Keep the count and chevron readable even when the caller requests a very narrow width.
        var chromeWidth = PreviewButton.Padding.Left + PreviewButton.Padding.Right +
                          PreviewButton.BorderThickness.Left + PreviewButton.BorderThickness.Right + 20;
        SetCurrentValue(MinWidthProperty, Math.Ceiling(MeasurePreview(_viewModel.OverflowLabel) + chromeWidth));
        _viewModel.UpdatePreview(PreviewText.Bounds.Width, MeasurePreview, PlaceholderText);
    }

    private double MeasurePreview(string text)
    {
        using var layout = new TextLayout(text,
            new Typeface(PreviewText.FontFamily, PreviewText.FontStyle, PreviewText.FontWeight, PreviewText.FontStretch),
            PreviewText.FontSize, Brushes.Black, flowDirection: PreviewText.FlowDirection,
            letterSpacing: PreviewText.LetterSpacing, fontFeatures: PreviewText.FontFeatures);
        return layout.WidthIncludingTrailingWhitespace;
    }

    private void ToggleDate(DateOnly date)
    {
        var dates = SelectedDates;
        if (dates == null)
        {
            dates = new ObservableCollection<DateOnly>();
            SetCurrentValue(SelectedDatesProperty, dates);
        }

        _updatingDates = true;
        try
        {
            if (dates.Contains(date))
            {
                for (var i = dates.Count - 1; i >= 0; i--)
                {
                    if (dates[i] == date)
                        dates.RemoveAt(i);
                }
            }
            else
            {
                dates.Add(date);
            }
        }
        finally
        {
            _updatingDates = false;
            UpdateDates();
        }
    }

    private void OnCalendarClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is CalendarDayButton { DataContext: DateTime date })
            ToggleDate(DateOnly.FromDateTime(date));
    }

    private void OnCalendarKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CalendarFlyout.Hide();
            PreviewButton.Focus();
            e.Handled = true;
            return;
        }
        if (MonthCalendar.DisplayMode != CalendarMode.Month || e.Key is not (Key.Enter or Key.Space))
            return;

        // Calendar owns keyboard focus; :dayfocused identifies its current date without selecting it.
        var focusedDay = MonthCalendar.GetVisualDescendants().OfType<CalendarDayButton>()
            .FirstOrDefault(x => x.Classes.Contains(":dayfocused"));
        if (focusedDay?.DataContext is DateTime date)
        {
            ToggleDate(DateOnly.FromDateTime(date));
            e.Handled = true;
        }
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Down or Key.F4)
        {
            CalendarFlyout.ShowAt(PreviewButton);
            e.Handled = true;
        }
    }

    private void OnFlyoutOpened(object? sender, EventArgs e)
    {
        var date = _viewModel.Dates.Count == 0 ? DateOnly.FromDateTime(DateTime.Today) : _viewModel.Dates.Min();
        MonthCalendar.SetCurrentValue(Calendar.DisplayDateProperty, date.ToDateTime(TimeOnly.MinValue));
        MonthCalendar.UpdateLayout();
        MonthCalendar.Focus(NavigationMethod.Tab);
    }
}
