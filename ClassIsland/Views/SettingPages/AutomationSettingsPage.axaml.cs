using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Core.Models;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Action;
using ClassIsland.ViewModels.SettingsPages;
using FluentAvalonia.UI.Controls;
namespace ClassIsland.Views.SettingPages;

/// <summary>
/// AutomationSettingsPage.axaml 的交互逻辑
/// </summary>
[FullWidthPage, HidePageTitle,
 SettingsPageInfo("automation", "自动化", "\ueEF1", "\ueEF0", SettingsPageCategory.Internal)]
public partial class AutomationSettingsPage : SettingsPageBase
{
    public AutomationSettingsViewModel ViewModel { get; } = IAppHost.GetService<AutomationSettingsViewModel>();

    public AutomationSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    void CommandRemove_OnExecuted(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: Workflow workflow })
            ViewModel.AutomationService.Workflows.Remove(workflow);
    }

    void CommandDuplicate_OnExecuted(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: Workflow workflow })
            ViewModel.AutomationService.Workflows.Insert(
                ViewModel.AutomationService.Workflows.IndexOf(workflow) + 1,
                ConfigureFileHelper.CopyObject(workflow)
            );
    }

    void CommandInvokeAction_OnExecuted(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: ActionSet actionSet })
            ViewModel.ActionService.InvokeActionSetAsync(actionSet);
    }

    void CommandRevertAction_OnExecuted(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: ActionSet actionSet })
            ViewModel.ActionService.RevertActionSetAsync(actionSet);
    }

    async void ButtonCreateConfig_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateProfileName = "";

        var textBox = new TextBox
        {
            Text = ""
        };
        var dialogResult = await new ContentDialog
        {
            Title = "新建自动化配置方案",
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = "创建",
            SecondaryButtonText = "取消",
            Content = new Field
            {
                Content = textBox,
                Label = "配置方案名称",
                Suffix = ".json"
            }
        }.ShowAsync();

        ViewModel.CreateProfileName = textBox.Text;
        var path = Path.Combine(AutomationService.AutomationConfigsFolderPath,
            ViewModel.CreateProfileName + ".json");
        if (dialogResult != ContentDialogResult.Primary || File.Exists(path))
        {
            return;
        }
        ConfigureFileHelper.SaveConfig(path, new ObservableCollection<Workflow>());
        ViewModel.AutomationService.RefreshConfigs();
        ViewModel.SettingsService.Settings.CurrentAutomationConfig = ViewModel.CreateProfileName;
    }

    void ButtonAdd_OnClick(object sender, RoutedEventArgs e)
    {
        var index = ViewModel.SelectedAutomation == null ? 
            ViewModel.AutomationService.Workflows.Count :
            ViewModel.AutomationService.Workflows.IndexOf(ViewModel.SelectedAutomation) + 1;
        var workflow = new Workflow { ActionSet = new() { Name = "新自动化" } };
        ViewModel.AutomationService.Workflows.Insert(index, workflow);
        ViewModel.SelectedAutomation = workflow;
        ViewModel.IsPanelOpened = true;
    }

    void SelectorMain_OnSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
        {
            ViewModel.IsPanelOpened = false;
            return;
        }
        ViewModel.IsPanelOpened = true;
    }

    void ButtonRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.AutomationService.RefreshConfigs();
    }

    void ButtonOpenConfigFolder_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Path.GetFullPath(AutomationService.AutomationConfigsFolderPath),
            UseShellExecute = true
        });
    }

    void ButtonAgreeAutomationNotice_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SettingsService.Settings.IsAutomationEnabled = true;
        ViewModel.SettingsService.Settings.IsAutomationWarningVisible = false;
    }
}
