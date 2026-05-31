using System.ComponentModel;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Tutorial;
using ClassIsland.Core.Models.UI;
using ClassIsland.Shared;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Core.Controls.Tutorial;

/// <summary>
/// 屏障控件。在教学进行时按需屏蔽输入。
/// </summary>
public class TutorialBarrier : ContentControl
{
    public static readonly StyledProperty<AvaloniaList<string>> ExcludedTagProperty = AvaloniaProperty.Register<TutorialBarrier, AvaloniaList<string>>(
        nameof(ExcludedTags));

    public AvaloniaList<string> ExcludedTags
    {
        get => GetValue(ExcludedTagProperty);
        set => SetValue(ExcludedTagProperty, value);
    }
    
    private ITutorialService TutorialService { get; } = IAppHost.GetService<ITutorialService>();

    private TutorialParagraph? ObservedParagraph { get; set; }

    private ToastMessage? _currentToastMessage;

    /// <inheritdoc />
    public TutorialBarrier()
    {
        ExcludedTags = [];
        Focusable = true;

        AddHandler(KeyDownEvent, OnInputEvent, RoutingStrategies.Tunnel, true);
        AddHandler(KeyUpEvent, OnInputEvent, RoutingStrategies.Tunnel, true);
        AddHandler(TextInputEvent, OnInputEvent, RoutingStrategies.Tunnel, true);
        AddHandler(PointerPressedEvent, OnInputEvent, RoutingStrategies.Tunnel, true);
        AddHandler(PointerReleasedEvent, OnInputEvent, RoutingStrategies.Tunnel, true);
        AddHandler(PointerMovedEvent, OnInputEventPointerMove, RoutingStrategies.Tunnel, true);
        AddHandler(PointerWheelChangedEvent, OnInputEventPointerMove, RoutingStrategies.Tunnel, true);
        AddHandler(PointerCaptureLostEvent, OnInputEvent, RoutingStrategies.Tunnel, true);
        AddHandler(GotFocusEvent, OnGotFocus, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        TutorialService.PropertyChanged += TutorialServiceOnPropertyChanged;
        UpdateObservedParagraph();
        MoveFocusOutOfContentIfNeeded();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        TutorialService.PropertyChanged -= TutorialServiceOnPropertyChanged;
        SetObservedParagraph(null);
        base.OnDetachedFromVisualTree(e);
    }

    private void TutorialServiceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(ITutorialService.CurrentParagraph))
        {
            return;
        }

        UpdateObservedParagraph();
        MoveFocusOutOfContentIfNeeded();
    }

    private void ParagraphOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TutorialParagraph.IsBarrierEnabled))
        {
            MoveFocusOutOfContentIfNeeded();
        }
    }

    private void UpdateObservedParagraph()
    {
        SetObservedParagraph(TutorialService.CurrentParagraph);
    }

    private void SetObservedParagraph(TutorialParagraph? paragraph)
    {
        if (ObservedParagraph == paragraph)
        {
            return;
        }

        if (ObservedParagraph != null)
        {
            ObservedParagraph.PropertyChanged -= ParagraphOnPropertyChanged;
        }

        ObservedParagraph = paragraph;
        if (ObservedParagraph != null)
        {
            ObservedParagraph.PropertyChanged += ParagraphOnPropertyChanged;
        }
    }

    private bool IsBarrierEnabled => TutorialService.CurrentParagraph?.IsBarrierEnabled == true && 
                                     !ExcludedTags.Contains(TutorialService.CurrentSentence?.Tag ?? "");

    private void OnInputEvent(object? sender, RoutedEventArgs e)
    {
        if (!IsBarrierEnabled)
        {
            return;
        }

        e.Handled = true;
        ShowBarrierTip();
        MoveFocusOutOfContentIfNeeded();
    }
    
    private void OnInputEventPointerMove(object? sender, RoutedEventArgs e)
    {
        if (!IsBarrierEnabled)
        {
            return;
        }

        e.Handled = true;
    }

    private void ShowBarrierTip()
    {
        if (_currentToastMessage is { IsOpen: true })
        {
            return;
        }
        
        var hyperlinkButton = new HyperlinkButton()
        {
            Content = "跳过教程",
        };
        var toastMessage = _currentToastMessage = new ToastMessage()
        {
            Title = "o((>ω< ))o 这里不能点！",
            Message = "教学正在进行中，等当前段落教学结束后再使用此功能吧。",
            ActionContent = hyperlinkButton,
            Duration = TimeSpan.FromSeconds(5),
            Severity = InfoBarSeverity.Warning
        };
        hyperlinkButton.Click += (sender, args) =>
        {
            toastMessage.Close();
            TutorialService.SkipTutorial();
        };
        this.ShowToast(toastMessage);
    }

    private void OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (!IsBarrierEnabled || e.Source == this)
        {
            return;
        }

        e.Handled = true;
        MoveFocusOutOfContentIfNeeded();
    }

    private void MoveFocusOutOfContentIfNeeded()
    {
        if (!IsBarrierEnabled || TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is not Visual focused ||
            !IsInThisVisualTree(focused) || focused == this)
        {
            return;
        }

        Focus();
    }

    private bool IsInThisVisualTree(Visual visual)
    {
        for (var current = visual; current != null; current = current.GetVisualParent())
        {
            if (current == this)
            {
                return true;
            }
        }

        return false;
    }
}
