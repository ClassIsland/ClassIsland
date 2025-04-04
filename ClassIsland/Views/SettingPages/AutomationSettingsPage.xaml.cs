using System.Windows;
using System.Windows.Input;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using ClassIsland.Shared.Helpers;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Linq;
using ClassIsland.Core.Models;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;
using ClassIsland.Shared.Models.Action;

namespace ClassIsland.Views.SettingPages;

/// <summary>
/// AutomationSettingsPage.xaml 的交互逻辑
/// </summary>
[SettingsPageInfo("automation", "自动化", PackIconKind.ScriptOutline, PackIconKind.ScriptText, SettingsPageCategory.Internal)]
public partial class AutomationSettingsPage
{
    public AutomationSettingsViewModel ViewModel { get; } = new();

    public IRulesetService RulesetService { get; }
    public SettingsService SettingsService { get; }
    public IAutomationService AutomationService { get; }
    public ILogger<AutomationSettingsPage> Logger { get; }

    public AutomationSettingsPage(IRulesetService rulesetService, SettingsService settingsService, ILogger<AutomationSettingsPage> logger, IAutomationService automationService)
    {
        RulesetService = rulesetService;
        SettingsService = settingsService;
        AutomationService = automationService;
        Logger = logger;
        DataContext = this;
        InitializeComponent();
    }

    public static readonly ICommand RemoveCommand = new RoutedUICommand();
    public static readonly ICommand DuplicateCommand = new RoutedUICommand();
    public static readonly ICommand DebugInvokeActionCommand = new RoutedUICommand();
    public static readonly ICommand DebugInvokeRevertActionCommand = new RoutedUICommand();

    private void ButtonAdd_OnClick(object sender, RoutedEventArgs e)
    {
        AutomationService.Workflows.Add(new() { ActionSet = new() { Name = "新自动化" } });
        ViewModel.SelectedAutomation = AutomationService.Workflows.Last();
        ViewModel.IsPanelOpened = true;
    }

    private void CommandRemove_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not Workflow automation) return;

        AutomationService.Workflows.Remove(automation);
    }

    private void CommandDuplicate_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not Workflow automation) return;

        AutomationService.Workflows.Insert(AutomationService.Workflows.IndexOf(automation) + 1, ConfigureFileHelper.CopyObject(automation));
    }

    private void CommandDebugInvokeAction_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not ActionSet actionset) return;

        App.GetService<IActionService>().Invoke(actionset);
    }

    private void CommandDebugInvokeRevertAction_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is not ActionSet actionset) return;

        App.GetService<IActionService>().Revert(actionset);
    }

    private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
        {
            ViewModel.IsPanelOpened = false;
            return;
        }
        ViewModel.IsPanelOpened = true;
    }

    private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!e.Handled)
        {
            // ListView拦截鼠标滚轮事件
            e.Handled = true;

            // 激发一个鼠标滚轮事件，冒泡给外层ListView接收到
            if (((Control)sender).Parent is UIElement parent)
            {
                parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = MouseWheelEvent,
                    Source = sender
                });
            }
        }
    }

    private void ButtonRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        AutomationService.RefreshConfigs();
    }

    private async void ButtonCreateConfig_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateProfileName = "";
        if (FindResource("CreateProfileDialog") is not FrameworkElement content)
            return;
        content.DataContext = this;
        var r = await DialogHost.Show(content, DialogHostIdentifier);
        Debug.WriteLine(r);

        var path = Path.Combine(Services.AutomationService.AutomationConfigsFolderPath,
            ViewModel.CreateProfileName + ".json");
        if (r == null || File.Exists(path))
        {
            return;
        }
        ConfigureFileHelper.SaveConfig(path, new ObservableCollection<Workflow>());
        AutomationService.RefreshConfigs();
        SettingsService.Settings.CurrentAutomationConfig = ViewModel.CreateProfileName;
        ViewModel.CreateProfileName = "Default";
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
        SettingsService.Settings.IsAutomationEnabled = true;
        SettingsService.Settings.IsAutomationWarningVisible = false;
    }
}