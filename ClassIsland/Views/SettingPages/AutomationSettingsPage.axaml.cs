using System.Windows;
using System.Windows.Input;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using ClassIsland.Shared.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Linq;
using ClassIsland.Core.Models;
using Avalonia.Controls;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Controls;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Action;
using FluentAvalonia.UI.Controls;
using ReactiveUI;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// AutomationSettingsPage.axaml 的交互逻辑
/// </summary>
[FullWidthPage]
[SettingsPageInfo("automation", "自动化", "\ueEF1", "\ueEF0", SettingsPageCategory.Internal)]
public partial class AutomationSettingsPage : SettingsPageBase
{
    public AutomationSettingsViewModel ViewModel { get; } = IAppHost.GetService<AutomationSettingsViewModel>();

    public AutomationSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }
    
    private void CommandRemove_OnExecuted(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: Workflow wf })
            ViewModel.AutomationService.Workflows.Remove(wf);
    }

    private void CommandDuplicate_OnExecuted(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: Workflow wf })
            ViewModel.AutomationService.Workflows.Insert(
                ViewModel.AutomationService.Workflows.IndexOf(wf) + 1,
                ConfigureFileHelper.CopyObject(wf)
            );
    }

    private void CommandInvokeAction_OnExecuted(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: ActionSet aset })
            ViewModel.ActionService.Invoke(aset);
    }

    private void CommandInvokeRevertAction_OnExecuted(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: ActionSet aset })
            ViewModel.ActionService.Revert(aset);
    }
    
    private async void ButtonCreateConfig_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateProfileName = "";

        var textBox = new TextBox()
        {
            Text = ""
        };
        var dialogResult = await new ContentDialog()
        {
            Title = "创建自动化配置",
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "创建",
            SecondaryButtonText = "取消",
            Content = new Field()
            {
                Content = textBox,
                Label = "组件名",
                Suffix = ".json"
            }
        }.ShowAsync();

        ViewModel.CreateProfileName = textBox.Text;
        var path = Path.Combine(ClassIsland.Services.AutomationService.AutomationConfigsFolderPath,
            ViewModel.CreateProfileName + ".json");
        if (dialogResult != ContentDialogResult.Primary || File.Exists(path))
        {
            return;
        }
        ConfigureFileHelper.SaveConfig(path, new ObservableCollection<Workflow>());
        ViewModel.AutomationService.RefreshConfigs();
        ViewModel.SettingsService.Settings.CurrentAutomationConfig = ViewModel.CreateProfileName;
    }

    private void ButtonAdd_OnClick(object sender, RoutedEventArgs e)
    {
        var index = ViewModel.SelectedAutomation == null ? 
            ViewModel.AutomationService.Workflows.Count :
            ViewModel.AutomationService.Workflows.IndexOf(ViewModel.SelectedAutomation) + 1;
        var workflow = new Workflow() { ActionSet = new() { Name = "新自动化" } };
        ViewModel.AutomationService.Workflows.Insert(index, workflow);
        ViewModel.SelectedAutomation = workflow;
        ViewModel.IsPanelOpened = true;
    }

    private void SelectorMain_OnSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
        {
            ViewModel.IsPanelOpened = false;
            return;
        }
        ViewModel.IsPanelOpened = true;
    }

    private void ButtonRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.AutomationService.RefreshConfigs();
    }

    private void ButtonOpenConfigFolder_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = Path.GetFullPath(Services.AutomationService.AutomationConfigsFolderPath),
            UseShellExecute = true
        });
    }

    private void ButtonAgreeAutomationNotice_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.IsAutomationEnabled = true;
        ViewModel.SettingsService.Settings.IsAutomationWarningVisible = false;
    }
}
// #endif
