using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Tutorial;
using ClassIsland.Shared;

namespace ClassIsland.Controls.Tutorial;

public class TutorialStateProvider : ContentControl
{
    public static readonly StyledProperty<Core.Models.Tutorial.Tutorial?> TutorialProperty = AvaloniaProperty.Register<TutorialStateProvider, Core.Models.Tutorial.Tutorial?>(
        nameof(Tutorial));

    public Core.Models.Tutorial.Tutorial? Tutorial
    {
        get => GetValue(TutorialProperty);
        set => SetValue(TutorialProperty, value);
    }

    public static readonly StyledProperty<TutorialParagraph?> ParagraphProperty = AvaloniaProperty.Register<TutorialStateProvider, TutorialParagraph?>(
        nameof(Paragraph));

    public TutorialParagraph? Paragraph
    {
        get => GetValue(ParagraphProperty);
        set => SetValue(ParagraphProperty, value);
    }

    public static readonly StyledProperty<bool> IsCompletedProperty = AvaloniaProperty.Register<TutorialStateProvider, bool>(
        nameof(IsCompleted));

    public bool IsCompleted
    {
        get => GetValue(IsCompletedProperty);
        set => SetValue(IsCompletedProperty, value);
    }

    public static readonly StyledProperty<int> CompletedParagraphsProperty = AvaloniaProperty.Register<TutorialStateProvider, int>(
        nameof(CompletedParagraphs));

    public int CompletedParagraphs
    {
        get => GetValue(CompletedParagraphsProperty);
        private set => SetValue(CompletedParagraphsProperty, value);
    }

    private ITutorialService TutorialService { get; } = IAppHost.GetService<ITutorialService>();

    public TutorialStateProvider()
    {
        this.GetObservable(TutorialProperty).Subscribe(_ => UpdateContent());
        this.GetObservable(ParagraphProperty).Subscribe(_ => UpdateContent());
        this.GetObservable(IsCompletedProperty).Subscribe(_ => UpdateContentReverse());
    }

    private void UpdateContentReverse()
    {
        if (Tutorial == null)
        {
            return;
        }
        var path = $"{Tutorial.Id}/{Paragraph?.Id}";
        var completed = TutorialService.GetIsTutorialCompleted(path);
        if (completed == IsCompleted)
        {
            return;
        }
        
        TutorialService.SetIsTutorialCompleted(path, IsCompleted);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        TutorialService.TutorialStateChanged += TutorialServiceOnTutorialStateChanged;
        UpdateContent();
    }
    
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        TutorialService.TutorialStateChanged -= TutorialServiceOnTutorialStateChanged;
    }
    
    private void TutorialServiceOnTutorialStateChanged(object? sender, EventArgs e)
    {
        UpdateContent();
    }

    private void UpdateContent()
    {
        if (Tutorial == null)
        {
            IsCompleted = false;
            return;
        }
        
        var completedCount = Tutorial.Paragraphs
            .Select(paragraph => $"{Tutorial.Id}/{paragraph.Id}")
            .Count(paragraphPath => TutorialService.GetIsTutorialCompleted(paragraphPath));
        CompletedParagraphs = completedCount;
        
        var path = $"{Tutorial.Id}/{Paragraph?.Id}";
        var completed = TutorialService.GetIsTutorialCompleted(path);
        if (completed == IsCompleted)
        {
            return;
        }

        IsCompleted = completed;
    }
}