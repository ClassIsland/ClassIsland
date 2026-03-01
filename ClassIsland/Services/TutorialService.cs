using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsyncImageLoader;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ClassIsland.Controls.Tutorial;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Models;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums.Tutorial;
using ClassIsland.Core.Extensions.UI;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Tutorial;
using ClassIsland.Models.Tutorial;
using ClassIsland.Shared.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using ReactiveUI;
using Tmds.DBus.Protocol;

namespace ClassIsland.Services;

public partial class TutorialService : ObservableObject, ITutorialService
{
    private SettingsService SettingsService { get; }
    private IActionService ActionService { get; }
    private IUriNavigationService UriNavigationService { get; }
    
    private TutorialSettings Settings { get; }

    [ObservableProperty] private Tutorial? _currentTutorial;
    [ObservableProperty] private TutorialParagraph? _currentParagraph;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(IsTutorialRunning))]
    private TutorialSentence? _currentSentence;

    private int SentenceIndex { get; set; } = -1;
    private int ParagraphIndex { get; set; } = -1;

    private List<Control> AttachedAdorners { get; } = [];
    
    private TeachingTip? CurrentTeachingTip { get; set; }
    
    private Border? CurrentDimBorder { get; set; }

    private bool _useDimPrev;

    private bool _isInvokingActions;

    [ObservableProperty] private TopLevel? _attachedToplevel;
    
    public event EventHandler? TutorialStateChanged;

    /// <inheritdoc/>
    public TutorialService(SettingsService settingsService, IActionService actionService, IUriNavigationService uriNavigationService)
    {
        SettingsService = settingsService;
        ActionService = actionService;
        UriNavigationService = uriNavigationService;

        Settings = ConfigureFileHelper.LoadConfig<TutorialSettings>(Path.Combine(CommonDirectories.AppConfigPath,
            "Tutorial.json"));
        Settings.PropertyChanged += (_, _) => SaveConfig();
        TutorialStateChanged += (_, _) => SaveConfig();
        AppBase.Current.AppStopping += (_, _) => SaveConfig();
        this.ObservableForProperty(x => x.IsTutorialRunning)
            .Subscribe(_ => TutorialStateChanged?.Invoke(this, EventArgs.Empty));

        if (SettingsService.WillMigrateInitTutorial)
        {
            SetIsTutorialCompleted("classisland.getStarted.welcome/init", true);
        }
    }

    public bool IsTutorialRunning => CurrentSentence != null;

    private void SaveConfig()
    {
        ConfigureFileHelper.SaveConfig(Path.Combine(CommonDirectories.AppConfigPath, "Tutorial.json"), Settings);
    }

    public void BeginTutorial(Tutorial tutorial)
    {
        if (IsTutorialRunning)
        {
            return;
        }
        
        JumpToParagraph(tutorial, null);
    }

    public void BeginTutorial(string path, bool requiresNotCompleted = false)
    {
        if (IsTutorialRunning)
        {
            return;
        }
        if (requiresNotCompleted && Settings.CompletedTutorials.Contains(path))
        {
            return;
        }

        var result = ParseParagraphPath(path, false);
        if (result is not {} v)
        {
            return;
        }
        var (tutorial, paragraph) = v;
        JumpToParagraph(tutorial, paragraph);
    }

    public void BeginNotCompletedTutorials(params string[] paths)
    {
        var target = paths.FirstOrDefault(x => !Settings.CompletedTutorials.Contains(x));
        if (target == null)
        {
            return;
        }
        BeginTutorial(target);
    }

    public void JumpToParagraph(Tutorial tutorial, TutorialParagraph? paragraph)
    {
        if (tutorial.Paragraphs.Count <= 0)
        {
            return;
        }

        TrySetCurrentParagraphCompleted();
        paragraph ??= tutorial.Paragraphs[0];
        ParagraphIndex = tutorial.Paragraphs.IndexOf(paragraph);
        StartParagraph(tutorial, paragraph);
    }

    public void PushToNextSentence(string? paragraphPath = null)
    {
        if (paragraphPath != null && GetCurrentParagraphPath() is {} currentPath && paragraphPath != currentPath)
        {
            return;
        }

        if (_isInvokingActions || CurrentSentence is not { WaitForNextCommand: true })
        {
            return;
        }
        
        TryStartNextSentence();
    }

    public void PushToNextSentenceByTag(string tag)
    {
        if (_isInvokingActions || CurrentSentence is not { WaitForNextCommand: true } sentence || sentence.Tag != tag)
        {
            return;
        }
        
        TryStartNextSentence();
    }

    public bool GetIsTutorialCompleted(string path)
    {
        return Settings.CompletedTutorials.Contains(path);
    }

    public void SetIsTutorialCompleted(string path, bool completed)
    {
        if (completed)
        {
            Settings.CompletedTutorials.Add(path);
        }
        else
        {
            Settings.CompletedTutorials.Remove(path);
        }
        TutorialStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ResetTutorialCompletedState()
    {
        Settings.CompletedTutorials.Clear();
        TutorialStateChanged?.Invoke(this, EventArgs.Empty);
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
        if (AttachedToplevel is { Content: Visual visual })
        {
            var layer = AdornerLayer.GetAdornerLayer(visual);
            if (CurrentTeachingTip != null)
            {
                CurrentTeachingTip.IsOpen = false;
                CurrentTeachingTip.Closed -= TeachingTipOnClosed;
            }

            if (CurrentDimBorder != null)
            {
                CurrentDimBorder.PointerReleased -= CurrentSpotlightOnClicked;
                CurrentDimBorder = null;
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

        if (CurrentTutorial == null || CurrentParagraph == null || AttachedToplevel == null)
        {
            return;
        }
        
        CurrentSentence = sentence;
        InvokeActions(sentence.InitializeActions, true);
        var targetControl = !string.IsNullOrWhiteSpace(sentence.TargetSelector)
            ? AttachedToplevel.FindDescendantBySelector(SelectorHelpers.Parse(sentence.TargetSelector,
                IXmlnsAttached.Combine([CurrentTutorial, CurrentParagraph, CurrentSentence]))) as Control
            : null;
        var heroContent = !string.IsNullOrWhiteSpace(sentence.HeroImage) ? new AdvancedImage(new Uri("avares://ClassIsland/"))
        {
            Source = sentence.HeroImage,
            CornerRadius = new CornerRadius(8, 8, 0, 0),
        } : null;
        if (heroContent != null)
        {
            RenderOptions.SetBitmapInterpolationMode(heroContent, BitmapInterpolationMode.HighQuality);
        }
        var teachingTip = CurrentTeachingTip = new TeachingTip()
        {
            HeroContent = heroContent,
            IconSource = sentence.IconSource,
            Title = sentence.Title,
            Subtitle = sentence.Content,
            Target = sentence.PointToTarget ? targetControl : null,
            PreferredPlacement = sentence.PlacementMode,
        };
        RenderOptions.SetBitmapInterpolationMode(teachingTip, BitmapInterpolationMode.HighQuality);
        if (!string.IsNullOrEmpty(sentence.LeftButtonText) && !sentence.UseLightDismiss)
        {
            teachingTip.ActionButtonContent = sentence.LeftButtonText;
            teachingTip.ActionButtonCommandParameter = sentence.LeftButtonActions;
        }
        if (!string.IsNullOrEmpty(sentence.RightButtonText) && !sentence.UseLightDismiss)
        {
            teachingTip.CloseButtonContent = sentence.RightButtonText;
            teachingTip.CloseButtonCommandParameter = sentence.RightButtonActions;
        }


        teachingTip.ActionButtonCommand = teachingTip.CloseButtonCommand = InvokeActionsCommand;

        AttachAdorner(teachingTip, AttachedToplevel);
        var useDim = sentence.ModalTarget;
        if (useDim)
        {
            var spotlight = new TutorialSpotlightControl()
            {
                FullscreenDim = targetControl == null,
                DisableIntro = _useDimPrev
            };
            AttachAdorner(spotlight, targetControl ?? AttachedToplevel);
        }
        _useDimPrev = useDim;
        
        if (sentence.HighlightTarget && targetControl != null)
        {
            var highlightControl = new TutorialHighlightControl();
            AttachAdorner(highlightControl, targetControl);
        }
        if (sentence.UseLightDismiss)
        {
            teachingTip.Closed += TeachingTipOnClosed;
            CurrentDimBorder = new Border()
            {
                Background = Brushes.Transparent
            };
            CurrentDimBorder.PointerReleased += CurrentSpotlightOnClicked;
            AttachAdorner(CurrentDimBorder, AttachedToplevel);
        }
        (AttachedToplevel as Window)?.Activate();
        Dispatcher.UIThread.Post(() => teachingTip.IsOpen = true);
    }

    private void CurrentSpotlightOnClicked(object? sender, EventArgs e)
    {
        if (CurrentSentence == null)
        {
            return;
        }
        InvokeActions(CurrentSentence.RightButtonActions);
    }

    private void TeachingTipOnClosed(TeachingTip sender, TeachingTipClosedEventArgs args)
    {
        if (CurrentSentence == null)
        {
            return;
        }
        InvokeActions(CurrentSentence.RightButtonActions);
    }

    private void TrySetCurrentParagraphCompleted()
    {
        if (CurrentParagraph != null && SentenceIndex + 1 >= CurrentParagraph.Content.Count && GetCurrentParagraphPath() is {} path) 
            Settings.CompletedTutorials.Add(path);
    }

    private (Tutorial, TutorialParagraph?)? ParseParagraphPath(string? paragraphPath, bool useCurrentContext)
    {
        if (paragraphPath == null)
        {
            return null;
        }

        var paths = paragraphPath.Split('/');
        if (useCurrentContext && paths.Length < 2)
        {
            if (CurrentTutorial == null)
            {
                return null;
            }
            var paragraph = CurrentTutorial.Paragraphs.FirstOrDefault(x => x.Id == paragraphPath);
            return (CurrentTutorial, paragraph);
        }
        if (paths.Length < 1)
        {
            return null;
        }
        var tutorial = ITutorialService.RegisteredTutorialGroups
            .SelectMany(x => x.Tutorials)
            .FirstOrDefault(x => x.Id == paths[0]);
        if (tutorial == null)
        {
            return null;
        }
        if (paths.Length < 2)
        {
            return (tutorial, null);
        }
        var paragraph1 = tutorial.Paragraphs.FirstOrDefault(x => x.Id == paths[1]);
        return (tutorial, paragraph1);
    }

    private string? GetCurrentParagraphPath()
    {
        if (CurrentTutorial != null && CurrentParagraph != null )
        {
            return $"{CurrentTutorial.Id}/{CurrentParagraph.Id}";
        }

        return null;
    }

    public void StopTutorial()
    {
        CleanupPrevSentence();
        TrySetCurrentParagraphCompleted();
        CurrentSentence = null;
        CurrentParagraph = null;
        CurrentTutorial = null;
        TutorialStateChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void InvokeActions(IList<TutorialAction> actions)
    {
        InvokeActions(actions, false);
    }

    private void InvokeActions(IList<TutorialAction> actions, bool isInit)
    {
        _isInvokingActions = true;
        foreach (var action in actions)
        {
            InvokeAction(action, isInit);
        }
        _isInvokingActions = false;
    }

    private void InvokeAction(TutorialAction action, bool isInit)
    {
        switch (action.Kind)
        {
            case TutorialActionKind.None:
                break;
            case TutorialActionKind.NextSentence when CurrentParagraph != null && !isInit:
                TryStartNextSentence();
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
                var result = ParseParagraphPath(action.StringParameter, true);
                if (result != null)
                {
                    var (tutorial, paragraph) = result.Value;
                    JumpToParagraph(tutorial, paragraph);
                }
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

    private void TryStartNextSentence()
    {
        if (CurrentParagraph != null && SentenceIndex + 1 < CurrentParagraph.Content.Count)
        {
            StartSentence(CurrentParagraph.Content[++SentenceIndex]);
            return;
        }
        if (TryStartNextParagraph())
        {
            return;
        }

        StopTutorial();
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
        
        TrySetCurrentParagraphCompleted();
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
        
        TrySetCurrentParagraphCompleted();
        StartParagraph(CurrentTutorial, CurrentTutorial.Paragraphs[--ParagraphIndex]);
        return true;
    }
}