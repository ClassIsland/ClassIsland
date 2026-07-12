// Source: https://github.com/amwx/FluentAvalonia/blob/a03fd9e02645c16ef0ebe7bb0266480eeb97871f/src/FluentAvalonia/UI/Controls/CommandBarFlyout/FATextCommandBarFlyout.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input.Platform;
using Avalonia.VisualTree;
using ClassIsland.Core.Assists;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Input;

namespace ClassIsland.Core.Controls;

/// <summary>
/// A text editing command bar flyout that presents its commands horizontally in touch mode.
/// </summary>
public class TouchTextCommandBarFlyout : FACommandBarFlyout
{
    private const double TextAnchorMargin = 8;

    public TouchTextCommandBarFlyout()
    {
        // Moving FATextCommandBarFlyout's generated commands from secondary to primary after the
        // template has been applied confuses FACommandBar's dynamic-overflow bookkeeping: its
        // overflow separator is removed and the next measure tries to move it from index -1.
        // The text flyout has at most six compact icon commands, so it does not need overflow.
        _commandBar.IsDynamicOverflowEnabled = false;
        Opening += (_, __) =>
        {
            UpdateButtons();
        };

        Opened += (_, __) =>
        {
            OnOpened();
        };
    }

    private new void OnOpened()
    {
        _targetLocal = new WeakReference<Control>(Target);

        // If there aren't any primary commands and we aren't opening expanded,
        // or if there are just no commands at all, then we'll have literally no UI to show. 
        // We'll just close the flyout in that case - nothing should be opening us
        // in this state anyway, but this makes sure we don't have a light-dismiss layer
        // with nothing visible to light dismiss.

        bool isStandard = ShowMode == FlyoutShowMode.Standard;


        if (PrimaryCommands.Count == 0 &&
            (SecondaryCommands.Count == 0) || (!_commandBar.IsOpen && !isStandard))
        {
            Hide();
        }
        _commandBar.IsOpen = false;
    }

    private void InitializeButtonWithUICommand(Button b,
        FAXamlUICommand command, Action executeFunc)
    {
        // WinUI collects the event token for revoking later, but never actually does
        // This should be ok because the button is tied to the TextCommandBarFlyout
        // so it's only created once and we shouldn't leak
        command.ExecuteRequested += (_, __) => { executeFunc(); };

        b.Command = command;
    }

    private void UpdateButtons()
    {
        PrimaryCommands.Clear();
        SecondaryCommands.Clear();

        var buttonsToAdd = GetButtonsToAdd();

        void addButtonToCommandsIfPresent(TextControlButtons buttonType, IList<IFACommandBarElement> commandsList)
        {
            if ((buttonsToAdd & buttonType) != TextControlButtons.None)
            {
                commandsList.Add(GetButton(buttonType));
            }
        }

        // No RichTextBox yet, so skipping this - Bold/Italic/Underline not supported yet
        //void addRichEditButtonToCommandsIfPresent(TextControlButtons buttonType, 
        //	IList<ICommandBarElement> commandsList, bool getIsChecked)
        //{
        //	if ((buttonsToAdd & buttonType) != TextControlButtons.None)
        //	{
        //		//TODO
        //	}
        //}

        // We don't have proofing flyouts, so skip that

        // We don't have FlyoutBase.InputDevicePrefersPrimaryCommands
        // So we'll always load Cut/Copy/Paste into Secondary
        // TODO_v2: We can implement InputDevicePrefersPrimaryCommands - pretty much that's touch
        addButtonToCommandsIfPresent(TextControlButtons.Cut, PrimaryCommands);
        addButtonToCommandsIfPresent(TextControlButtons.Copy, PrimaryCommands);
        addButtonToCommandsIfPresent(TextControlButtons.Paste, PrimaryCommands);
        
        //TODO: the bool arg
        //addRichEditButtonToCommandsIfPresent(TextControlButtons.Bold, PrimaryCommands, false);
        //addRichEditButtonToCommandsIfPresent(TextControlButtons.Italic, PrimaryCommands, false);
        //addRichEditButtonToCommandsIfPresent(TextControlButtons.Underline, PrimaryCommands, false);

        addButtonToCommandsIfPresent(TextControlButtons.Undo, SecondaryCommands);
        addButtonToCommandsIfPresent(TextControlButtons.Redo, SecondaryCommands);
        addButtonToCommandsIfPresent(TextControlButtons.SelectAll, SecondaryCommands);
    }

    private TextControlButtons GetButtonsToAdd()
    {
        TextControlButtons toAdd = TextControlButtons.None;
        var target = Target;

        // Since we don't have RichTextBox, RichTextBlock, or PasswordBox, we'll just let TextBox get all
        // of those where appropriate. TextBlock will stay the same. Basically no Bold/Italic/Underline
        // commands until RichTextBox is added to Avalonia
        if (target is TextBox tbTarget)
        {
            if (tbTarget.PasswordChar != default(char))
            {
                toAdd = GetPasswordBoxButtonsToAdd(tbTarget);
            }
            else
            {
                toAdd = GetTextBoxButtonsToAdd(tbTarget);
            }
        }
        else if (target is TextBlock txtTarget) // This also handles SelectableTextBlock
        {
            toAdd = GetTextBlockButtonsToAdd(txtTarget);
        }

        return toAdd;
    }

    private TextControlButtons GetTextBoxButtonsToAdd(TextBox textBox)
    {
        TextControlButtons toAdd = TextControlButtons.None;

        var selLength = Math.Abs(textBox.SelectionEnd - textBox.SelectionStart);
        if (!textBox.IsReadOnly)
        {
            if (selLength > 0)
            {
                toAdd |= TextControlButtons.Cut;
            }

            if (textBox.CanPaste)
            {
                toAdd |= TextControlButtons.Paste;
            }

            // We don't have CanUndo or CanRedo
            // In next verion of Avalonia, we'll get TextBox.IsUndoEnabled, but it's not the same
            // For now, we'll default to adding these, and probably just send the Undo/Redo keys

            if (textBox.CanUndo)
            {
                toAdd |= TextControlButtons.Undo;
            }

            if (textBox.CanRedo)
            {
                toAdd |= TextControlButtons.Undo;
            }
        }

        if (selLength > 0)
        {
            toAdd |= TextControlButtons.Copy;
        }

        if (!string.IsNullOrEmpty(textBox.Text) && textBox.Text.Length > 0)
        {
            toAdd |= TextControlButtons.SelectAll;
        }

        return toAdd;
    }

    private TextControlButtons GetTextBlockButtonsToAdd(TextBlock tb)
    {
        // TextBlocks aren't as robust as WinUI, but we should still be able 
        // to make Copy work. SelectAll won't though

        var buttonsToAdd = TextControlButtons.None;

        if (tb is SelectableTextBlock stb)
        {
            var selLength = Math.Abs(stb.SelectionEnd - stb.SelectionStart);
            if (selLength > 0)
            {
                buttonsToAdd |= TextControlButtons.Copy;
            }
            if (!string.IsNullOrEmpty(stb.Text) && stb.Text.Length > 0)
                buttonsToAdd |= TextControlButtons.SelectAll;
        }
        else
        {
            buttonsToAdd |= TextControlButtons.Copy;
        }
        
        return buttonsToAdd;
    }

    //private TextControlButtons GetRichEditBoxButtonsToAdd() { }
    //private TextControlButtons GetRichTextBlockButtonsToAdd() { }

    private TextControlButtons GetPasswordBoxButtonsToAdd(TextBox textBox)
    {
        TextControlButtons toAdd = TextControlButtons.None;

        if (textBox.CanPaste)
        {
            toAdd |= TextControlButtons.Paste;
        }

        if (!string.IsNullOrEmpty(textBox.Text) && textBox.Text.Length > 0)
        {
            toAdd |= TextControlButtons.SelectAll;
        }

        return toAdd;
    }

    private bool IsButtonInPrimaryCommands(TextControlButtons button)
    {
        return PrimaryCommands.Contains(GetButton(button));
    }

    private void ExecuteCutCommand()
    {
        if (_targetLocal.TryGetTarget(out var target))
        {
            try
            {
                if (target is TextBox tb)
                {
                    tb.Cut();
                }
            }
            catch
            {
                // TODO: probably should log the error if one is thrown, but don't fail b/c of it
                // Clipboard errors do happen
            }

            if (IsButtonInPrimaryCommands(TextControlButtons.Cut))
            {
                UpdateButtons();
            }
        }        
        Hide();
    }

    private async void ExecuteCopyCommand()
    {
        if (_targetLocal.TryGetTarget(out var target))
        {
            try
            {
                if (target is TextBox tb)
                {
                    tb.Copy();
                }
                else if (target is SelectableTextBlock stb)
                {
                    stb.Copy();
                }
                else if (target is TextBlock txtB)
                {
                    await TopLevel.GetTopLevel(Target).Clipboard.SetTextAsync(txtB.Text);
                }
            }
            catch
            {
                // TODO: probably should log the error if one is thrown, but don't fail b/c of it
                // Clipboard errors do happen
            }

            if (IsButtonInPrimaryCommands(TextControlButtons.Copy))
            {
                UpdateButtons();
            }
        }        
        Hide();
    }

    private async void ExecutePasteCommand()
    {
        if (_targetLocal.TryGetTarget(out var target))
        {
            try
            {
                if (target is TextBox tb)
                {
                    tb.Paste();
                }
                else if (target is TextBlock txtB)
                {
                    var txt = await ClipboardExtensions.TryGetTextAsync(TopLevel.GetTopLevel(target).Clipboard);
                    if (txt != null)
                    {
                        txtB.Text = txt;
                    }
                }
            }
            catch
            {
                // TODO: probably should log the error if one is thrown, but don't fail b/c of it
                // Clipboard errors do happen
            }

            if (IsButtonInPrimaryCommands(TextControlButtons.Paste))
            {
                UpdateButtons();
            }
        }           
        Hide();
    }

    private void ExecuteBoldCommand()
    { }

    private void ExecuteItalicCommand()
    { }

    private void ExecuteUnderlineCommand()
    { }

    private void ExecuteUndoCommand()
    {
        if (_targetLocal.TryGetTarget(out var target) && target is TextBox tb)
        {
            tb.Undo();
        }

        if (IsButtonInPrimaryCommands(TextControlButtons.Undo))
        {
            UpdateButtons();
        }
    }

    private void ExecuteRedoCommand()
    {
        if (_targetLocal.TryGetTarget(out var target) && target is TextBox tb)
        {
            tb.Redo();
        }

        if (IsButtonInPrimaryCommands(TextControlButtons.Redo))
        {
            UpdateButtons();
        }
    }

    private void ExecuteSelectAllCommand()
    {
        if (_targetLocal.TryGetTarget(out var target) && target is TextBox tb)
        {
            tb.SelectAll();
        }
        else if (target is SelectableTextBlock stb)
        {
            stb.SelectAll();
        }

        if (IsButtonInPrimaryCommands(TextControlButtons.SelectAll))
        {
            UpdateButtons();
        }
    }

    private IFACommandBarElement GetButton(TextControlButtons textControlButton)
    {
        if (_buttons == null)
            _buttons = new Dictionary<TextControlButtons, IFACommandBarElement>();

        if (_buttons.ContainsKey(textControlButton))
        {
            return _buttons[textControlButton];
        }
        else
        {
            switch (textControlButton)
            {
                case TextControlButtons.Cut:
                    {
                        var button = new FACommandBarButton();
                        InitializeButtonWithUICommand(button, new FAStandardUICommand(FAStandardUICommandKind.Cut), ExecuteCutCommand);
                        _buttons.Add(TextControlButtons.Cut, button);
                        return button;
                    }

                case TextControlButtons.Copy:
                    {
                        var button = new FACommandBarButton();
                        InitializeButtonWithUICommand(button, new FAStandardUICommand(FAStandardUICommandKind.Copy), ExecuteCopyCommand);
                        _buttons.Add(TextControlButtons.Copy, button);
                        return button;
                    }

                case TextControlButtons.Paste:
                    {
                        var button = new FACommandBarButton();
                        InitializeButtonWithUICommand(button, new FAStandardUICommand(FAStandardUICommandKind.Paste), ExecutePasteCommand);
                        _buttons.Add(TextControlButtons.Paste, button);
                        return button;
                    }

                // Skip Bold/Italic/Underline, since we don't have those right now

                case TextControlButtons.Bold:
                case TextControlButtons.Italic:
                case TextControlButtons.Underline:
                    return null;

                case TextControlButtons.Undo:
                    {
                        var button = new FACommandBarButton();
                        InitializeButtonWithUICommand(button, new FAStandardUICommand(FAStandardUICommandKind.Undo), ExecuteUndoCommand);
                        _buttons.Add(TextControlButtons.Undo, button);
                        return button;
                    }

                case TextControlButtons.Redo:
                    {
                        var button = new FACommandBarButton();
                        InitializeButtonWithUICommand(button, new FAStandardUICommand(FAStandardUICommandKind.Redo), ExecuteRedoCommand);
                        _buttons.Add(TextControlButtons.Redo, button);
                        return button;
                    }

                case TextControlButtons.SelectAll:
                    {
                        var button = new FACommandBarButton();
                        InitializeButtonWithUICommand(button, new FAStandardUICommand(FAStandardUICommandKind.SelectAll), ExecuteSelectAllCommand);
                        _buttons.Add(TextControlButtons.SelectAll, button);
                        return button;
                    }

                default:
                    throw new NotSupportedException("Invalid TextControlButtons");
            }
        }
    }

    private Dictionary<TextControlButtons, IFACommandBarElement> _buttons;
    private WeakReference<Control> _targetLocal;

    protected override bool ShowAtCore(Control placementTarget, bool showAtPointer = false)
    {
        if (placementTarget is TextBox textBox &&
            PointerStateAssist.GetIsTouchMode(textBox) &&
            TryGetTextAnchor(textBox, out var anchor))
        {
            Placement = PlacementMode.Custom;
            CustomPopupPlacementCallback = parameters => PlaceAroundText(parameters, textBox, anchor);

            // Context flyouts normally force pointer placement for a hold gesture. Text editing
            // commands should instead follow the selection or caret line calculated above.
            showAtPointer = false;
        }

        return base.ShowAtCore(placementTarget, showAtPointer);
    }

    private static bool TryGetTextAnchor(TextBox textBox, out Rect anchor)
    {
        anchor = default;

        var presenter = textBox.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();
        if (presenter is null)
        {
            return false;
        }

        var selectionStart = Math.Min(textBox.SelectionStart, textBox.SelectionEnd);
        var selectionLength = Math.Abs(textBox.SelectionEnd - textBox.SelectionStart);
        Rect? presenterAnchor = null;

        if (selectionLength > 0)
        {
            foreach (var selectionRect in presenter.TextLayout.HitTestTextRange(selectionStart, selectionLength))
            {
                presenterAnchor = presenterAnchor?.Union(selectionRect) ?? selectionRect;
            }
        }
        else
        {
            var caretRect = presenter.TextLayout.HitTestTextPosition(textBox.CaretIndex);
            presenterAnchor = new Rect(caretRect.X, caretRect.Y, 1, caretRect.Height);
        }

        if (presenterAnchor is not { Height: > 0 } textAnchor ||
            presenter.TranslatePoint(textAnchor.TopLeft, textBox) is not { } topLeft ||
            presenter.TranslatePoint(textAnchor.BottomRight, textBox) is not { } bottomRight)
        {
            return false;
        }

        var visibleBounds = new Rect(textBox.Bounds.Size);
        anchor = new Rect(topLeft, bottomRight).Intersect(visibleBounds);
        return anchor.Height > 0;
    }

    private static void PlaceAroundText(CustomPopupPlacement parameters, TextBox textBox, Rect anchor)
    {
        var root = TopLevel.GetTopLevel(textBox);
        var transform = root is null ? null : textBox.TransformToVisual(root);

        if (transform is not null)
        {
            parameters.AnchorRectangle = anchor
                .Inflate(new Thickness(0, TextAnchorMargin))
                .TransformToAABB(transform.Value);
        }

        parameters.Anchor = PopupAnchor.Top;
        parameters.Gravity = PopupGravity.Top;
        parameters.ConstraintAdjustment = PopupPositionerConstraintAdjustment.FlipY |
                                          PopupPositionerConstraintAdjustment.SlideX;
    }
    
    internal enum TextControlButtons
    {
        None = 0x0000,
        Cut = 0x0001,
        Copy = 0x0002,
        Paste = 0x0004,
        Bold = 0x0008,
        Italic = 0x0010,
        Underline = 0x0020,
        Undo = 0x0040,
        Redo = 0x0080,
        SelectAll = 0x0100
    }
}
