using System;
using System.Collections.Generic;
using System.Linq;
using AsyncImageLoader;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using ClassIsland.Controls.Tutorial;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums.Tutorial;
using ClassIsland.Core.Extensions.UI;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Tutorial;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Tmds.DBus.Protocol;

namespace ClassIsland.Services;

public partial class TutorialService(SettingsService settingsService, IActionService actionService, IUriNavigationService uriNavigationService) : ObservableObject, ITutorialService
{
    private SettingsService SettingsService { get; } = settingsService;
    private IActionService ActionService { get; } = actionService;
    private IUriNavigationService UriNavigationService { get; } = uriNavigationService;

    [ObservableProperty] private Tutorial? _currentTutorial;
    [ObservableProperty] private TutorialParagraph? _currentParagraph;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(IsTutorialRunning))]
    private TutorialSentence? _currentSentence;

    private int SentenceIndex { get; set; } = -1;
    private int ParagraphIndex { get; set; } = -1;

    private List<Control> AttachedAdorners { get; } = [];
    
    private TeachingTip? CurrentTeachingTip { get; set; }

    [ObservableProperty] private TopLevel? _attachedToplevel;
    
    public bool IsTutorialRunning => CurrentSentence != null;

    public void BeginTutorial(Tutorial tutorial)
    {
        // if (CurrentTutorial != null)
        // {
        //     return;
        // }
        
        JumpToParagraph(tutorial, null);
    }

    public void JumpToParagraph(Tutorial tutorial, TutorialParagraph? paragraph)
    {
        if (tutorial.Paragraphs.Count <= 0)
        {
            return;
        }

        paragraph ??= tutorial.Paragraphs[0];
        ParagraphIndex = tutorial.Paragraphs.IndexOf(paragraph);
        StartParagraph(tutorial, paragraph);
    }

    private void StartParagraph(Tutorial tutorial, TutorialParagraph paragraph)
    {
        CleanupPrevSentence();
        CurrentTutorial = tutorial;
        CurrentParagraph = paragraph;
        SentenceIndex = 0;
        InvokeActions(paragraph.InitializeActions, true);
        var topLevel =
            AppBase.Current.DesktopLifetime?.Windows.FirstOrDefault(x =>
                x.GetType().FullName == paragraph.TopLevelClassName);
        AttachedToplevel = topLevel;
        if (paragraph.Content.Count <= 0)
        {
            return;
        }
        StartSentence(paragraph.Content[0]);
    }

    private void CleanupPrevSentence()
    {
        if (AttachedToplevel != null && AttachedToplevel.Content is Visual visual)
        {
            var layer = AdornerLayer.GetAdornerLayer(visual);
            if (CurrentTeachingTip != null)
            {
                CurrentTeachingTip.IsOpen = false;
            }

            foreach (var adorner in AttachedAdorners)
            {
                layer?.Children.Remove(adorner);
            }
        }

        CurrentTeachingTip = null;
        AttachedAdorners.Clear();
    }

    private void AttachAdorner(Control adorner, Control target)
    {
        if (AttachedToplevel?.Content is not Visual visual)
        {
            return;
        }
        var layer = AdornerLayer.GetAdornerLayer(visual);
        AdornerLayer.SetIsClipEnabled(adorner, false);
        layer?.Children.Add(adorner);
        AdornerLayer.SetAdornedElement(adorner, target);
        AttachedAdorners.Add(adorner);
    }

    private void StartSentence(TutorialSentence sentence)
    {
        CleanupPrevSentence();

        if (AttachedToplevel == null || AttachedToplevel.Content is not Visual visual)
        {
            return;
        }
        CurrentSentence = sentence;
        InvokeActions(sentence.InitializeActions, true);
        var targetControl = !string.IsNullOrWhiteSpace(sentence.TargetSelector)
            ? AttachedToplevel.FindDescendantBySelector(SelectorHelpers.Parse(sentence.TargetSelector,
                new Dict<string, string>())) as Control
            : null;
        var teachingTip = CurrentTeachingTip = new TeachingTip()
        {
            HeroContent = !string.IsNullOrWhiteSpace(sentence.HeroImage) ? new AdvancedImage(new Uri("avares://ClassIsland/"))
            {
                Source = sentence.HeroImage,
                CornerRadius = new CornerRadius(8, 8, 0, 0)
            } : null,
            IconSource = sentence.IconSource,
            Title = sentence.Title,
            Subtitle = sentence.Content,
            Target = sentence.PointToTarget ? targetControl : null
        };
        if (!string.IsNullOrEmpty(sentence.LeftButtonText))
        {
            teachingTip.ActionButtonContent = sentence.LeftButtonText;
            teachingTip.ActionButtonCommandParameter = sentence.LeftButtonActions;
        }
        if (!string.IsNullOrEmpty(sentence.RightButtonText))
        {
            teachingTip.CloseButtonContent = sentence.RightButtonText;
            teachingTip.CloseButtonCommandParameter = sentence.RightButtonActions;
        }

        teachingTip.ActionButtonCommand = teachingTip.CloseButtonCommand = InvokeActionsCommand;

        AttachAdorner(teachingTip, AttachedToplevel);
        if (sentence.ModalTarget && targetControl != null)
        {
            var spotlight = new TutorialSpotlightControl();
            AttachAdorner(spotlight, targetControl);
        }
        if (sentence.HighlightTarget && targetControl != null)
        {
            var highlightControl = new TutorialHighlightControl();
            AttachAdorner(highlightControl, targetControl);
        }
        (AttachedToplevel as Window)?.Activate();
        Dispatcher.UIThread.Post(() => teachingTip.IsOpen = true);
    }

    public void StopTutorial()
    {
        CleanupPrevSentence();
    }

    [RelayCommand]
    public void InvokeActions(IList<TutorialAction> actions)
    {
        InvokeActions(actions, false);
    }

    private void InvokeActions(IList<TutorialAction> actions, bool isInit)
    {
        foreach (var action in actions)
        {
            InvokeAction(action, isInit);
        }
    }

    private void InvokeAction(TutorialAction action, bool isInit)
    {
        switch (action.Kind)
        {
            case TutorialActionKind.None:
                break;
            case TutorialActionKind.NextSentence when CurrentParagraph != null && !isInit:
                if (SentenceIndex + 1 < CurrentParagraph.Content.Count)
                {
                    StartSentence(CurrentParagraph.Content[++SentenceIndex]);
                    break;
                }
                if (TryStartNextParagraph())
                {
                    break;
                }

                StopTutorial();
                break;
            case TutorialActionKind.NextParagraph when !isInit:
                if (TryStartNextParagraph())
                {
                    break;
                }

                StopTutorial();
                break;
            case TutorialActionKind.PreviousSentence when CurrentParagraph != null:
                if (SentenceIndex - 1 >= 0)
                {
                    StartSentence(CurrentParagraph.Content[--SentenceIndex]);
                    break;
                }
                if (TryStartPreviousParagraph())
                {
                    break;
                }

                StopTutorial();
                break;
            case TutorialActionKind.PreviousParagraph when !isInit:
                if (TryStartPreviousParagraph())
                {
                    break;
                }

                StopTutorial();
                break;
            case TutorialActionKind.JumpParagraph when !isInit:
                break;
            case TutorialActionKind.Stop:
                StopTutorial();
                break;
            case TutorialActionKind.InvokeActionSet when action.ActionSet != null:
                ActionService.InvokeActionSetAsync(action.ActionSet);
                break;
            case TutorialActionKind.InvokeUri:
                UriNavigationService.NavigateWrapped(new Uri(action.StringParameter));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool TryStartNextParagraph()
    {
        if (CurrentTutorial == null)
        {
            return false;
        }
        if (ParagraphIndex + 1 >= CurrentTutorial.Paragraphs.Count)
        {
            return false;
        }
        
        StartParagraph(CurrentTutorial, CurrentTutorial.Paragraphs[++ParagraphIndex]);
        return true;
    }
    
    private bool TryStartPreviousParagraph()
    {
        if (CurrentTutorial == null)
        {
            return false;
        }
        if (ParagraphIndex - 1 < 0)
        {
            return false;
        }
        
        StartParagraph(CurrentTutorial, CurrentTutorial.Paragraphs[--ParagraphIndex]);
        return true;
    }
}