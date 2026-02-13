using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums.Tutorial;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.Tutorial;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.ViewModels;
using DynamicData;
using FluentAvalonia.UI.Controls;
using HotAvalonia;
using Mono.Unix;

namespace ClassIsland.Views;

public partial class TutorialEditorWindow : MyWindow
{
    public static FuncValueConverter<TutorialActionKind, int> TutorialActionKindToIntConverter { get; } =
        new(x => (int)x, x => (TutorialActionKind)x);
    
    public TutorialEditorViewModel ViewModel { get; } = IAppHost.GetService<TutorialEditorViewModel>();
    
    public TutorialEditorWindow()
    {
        InitializeComponent();
        DataContext = this;

        SetupEventHandlers();
    }
    
    [AvaloniaHotReload]
    private void SetupEventHandlers()
    {
        ListBoxTutorials.SelectionChanged += ListBoxTutorials_OnGotFocus;
        ListBoxTutorials.GotFocus += ListBoxTutorials_OnGotFocus;
        ListBoxParagraphs.SelectionChanged += ListBoxParagraphs_OnGotFocus;
        ListBoxParagraphs.GotFocus += ListBoxParagraphs_OnGotFocus;
        DataGridParagraphContent.SelectionChanged += DataGridParagraphContent_OnGotFocus;
        DataGridParagraphContent.GotFocus += DataGridParagraphContent_OnGotFocus;
    }
    
    private void ButtonAddTutorial_OnClick(object? sender, RoutedEventArgs e)
    {
        var tutorial = new Tutorial()
        {
            Title = "新教程"
        };
        ViewModel.CurrentTutorialGroup.Tutorials.Add(tutorial);
        ViewModel.CurrentTutorial = tutorial;
    }

    private void ButtonDuplicateTutorial_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentTutorial != null)
            ViewModel.CurrentTutorialGroup.Tutorials.Add(ConfigureFileHelper.CopyObject(ViewModel.CurrentTutorial));
    }
    
    private void ButtonDeleteTutorial_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentTutorial != null)
            ViewModel.CurrentTutorialGroup.Tutorials.Remove(ViewModel.CurrentTutorial);
    }

    private void ListBoxTutorials_OnGotFocus(object? sender, RoutedEventArgs e)
    {
        ViewModel.SelectedDetailObject = ViewModel.CurrentTutorial;
    }
    

    private void ButtonAddParagraph_OnClick(object? sender, RoutedEventArgs e)
    {
        var tutorialParagraph = new TutorialParagraph()
        {
            Title = "新段落"
        };
        ViewModel.CurrentTutorial?.Paragraphs.Add(tutorialParagraph);
        ViewModel.CurrentParagraph = tutorialParagraph;
    }

    private void ButtonDuplicateParagraph_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentParagraph != null) 
            ViewModel.CurrentTutorial?.Paragraphs.Add(ConfigureFileHelper.CopyObject(ViewModel.CurrentParagraph));
    }
    
    private void ButtonDeleteParagraph_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentParagraph != null) 
            ViewModel.CurrentTutorial?.Paragraphs.Remove(ViewModel.CurrentParagraph);
    }
    
    private void ListBoxParagraphs_OnGotFocus(object? sender, RoutedEventArgs e)
    {
        ViewModel.SelectedDetailObject = ViewModel.CurrentParagraph;
    }

    private void ButtonAddSentence_OnClick(object? sender, RoutedEventArgs e)
    {
        var tutorialSentence = new TutorialSentence();
        ViewModel.CurrentParagraph?.Content.Add(tutorialSentence);
        ViewModel.CurrentSentence = tutorialSentence;
    }

    private void ButtonDuplicateSentence_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentSentence != null) 
            ViewModel.CurrentParagraph?.Content.Add(ConfigureFileHelper.CopyObject(ViewModel.CurrentSentence));
    }
    
    private void ButtonDeleteSentence_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentSentence != null) 
            ViewModel.CurrentParagraph?.Content.Remove(ViewModel.CurrentSentence);
    }
    
    private void DataGridParagraphContent_OnGotFocus(object? sender, RoutedEventArgs e)
    {
        ViewModel.SelectedDetailObject = ViewModel.CurrentSentence ?? ViewModel.SelectedDetailObject;
    }

    private void MenuItemRunCurrentTutorial_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentTutorial != null) 
            ViewModel.TutorialService.BeginTutorial(ViewModel.CurrentTutorial);
    }

    private void MenuItemRunCurrentParagraph_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentTutorial != null) 
            ViewModel.TutorialService.JumpToParagraph(ViewModel.CurrentTutorial, ViewModel.CurrentParagraph);
    }

    private void MenuItemStopTutorial_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.TutorialService.StopTutorial();
    }

    private async void MenuItemOpen_OnClick(object? sender, RoutedEventArgs e)
    {
        var paths = await PlatformServices.FilePickerService.OpenFilesPickerAsync(new FilePickerOpenOptions
        {
            FileTypeFilter = [FilePickerFileTypes.Json],
            AllowMultiple = false
        }, this);
        if (paths.Count <= 0)
        {
            return;
        }

        var path = ViewModel.OpenedFilePath = paths[0];
        ViewModel.CurrentTutorialGroup = ConfigureFileHelper.LoadConfig<TutorialGroup>(path, false);
        this.ShowToast($"已打开 {path}");
        var id = ViewModel.CurrentTutorialGroup.Id;
        if (ITutorialService.RegisteredTutorialGroups.FirstOrDefault(x => x.Id == id) is not {} existed)
        {
            return;
        }
        ViewModel.TutorialService.StopTutorial();
        ITutorialService.RegisteredTutorialGroups.Replace(existed, ViewModel.CurrentTutorialGroup);
        this.ShowToast($"已替换 ID 为 {id} 的教程组并启用热重载。");
    }

    private async void MenuItemSave_OnClick(object? sender, RoutedEventArgs e)
    {
        await SaveTutorialGroupFull();
    }

    private async Task SaveTutorialGroupFull(bool saveAs=false)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.OpenedFilePath) || saveAs)
        {
            var paths = await PlatformServices.FilePickerService.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                FileTypeChoices = [FilePickerFileTypes.Json],
            }, this);
            if (paths == null)
            {
                return;
            }

            ViewModel.OpenedFilePath = paths;
        }

        SaveTutorialGroupToFile();
    }

    private void SaveTutorialGroupToFile()
    {
        if (string.IsNullOrWhiteSpace(ViewModel.OpenedFilePath))
        {
            return;
        }
        ConfigureFileHelper.SaveConfig(ViewModel.OpenedFilePath, ViewModel.CurrentTutorialGroup, true);
        this.ShowToast($"已保存 {ViewModel.OpenedFilePath}");
    }

    private async void TopLevel_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown || ViewModel.IsClosing)
        {
            return;
        }
        e.Cancel = true;
        Dispatcher.UIThread.Post(async () => await ClosingCore());
    }

    private async Task ClosingCore()
    {
        if (string.IsNullOrWhiteSpace(ViewModel.OpenedFilePath))
        {
            var dialog = new TaskDialog()
            {
                XamlRoot = this,
                Header = "更改尚未保存",
                Content = "更改尚未保存，您要在退出前保存更改吗？",
                Buttons =
                [
                    new TaskDialogButton("保存", 0)
                    {
                        IsDefault = true,
                    },
                    new TaskDialogButton("不保存", 1),
                    new TaskDialogButton("取消", 2)
                ]
            };
            var result = await dialog.ShowAsync();
            if (result is not int choice)
            {
                return;
            }

            switch (choice)
            {
                case 0:
                    await SaveTutorialGroupFull();
                    if (string.IsNullOrWhiteSpace(ViewModel.OpenedFilePath))
                    {
                        return;
                    }
                    break;
                case 1:
                    break;
                case 2:
                    return;
                default:
                    break;
            }
        }
        SaveTutorialGroupToFile();
        ViewModel.IsClosing = true;
        Close();
    }

    private void WindowBase_OnDeactivated(object? sender, EventArgs e)
    {
        SaveTutorialGroupToFile();
    }

    private async void MenuItemSaveAs_OnClick(object? sender, RoutedEventArgs e)
    {
        await SaveTutorialGroupFull(true);
    }

    private void MenuItemTutorialGroupInfo_OnClick(object? sender, RoutedEventArgs e)
    {
        ViewModel.SelectedDetailObject = ViewModel.CurrentTutorialGroup;
    }
}