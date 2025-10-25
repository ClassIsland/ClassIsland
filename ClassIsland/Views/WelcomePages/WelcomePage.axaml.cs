using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ClassIsland.Controls;
using ClassIsland.Core;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Core.Models.UI;
using ClassIsland.ViewModels;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views.WelcomePages;

public partial class WelcomePage : UserControl, IWelcomePage
{
    public WelcomeViewModel ViewModel { get; set; } = null!;
    
    public WelcomePage()
    {
        InitializeComponent();
    }

    private void ButtonNext_OnClick(object? sender, RoutedEventArgs e)
    {
        NavigateNext();
    }

    private void NavigateNext()
    {
        this.ShowToast(new ToastMessage()
        {
            Title = "欢迎使用 ClassIsland",
            Message = "ClassIsland 是开源免费的软件，官方没有提供任何形式的付费支持服务，源代码仓库地址在 https://github.com/ClassIsland/ClassIsland/。如果您通过有偿协助等付费方式取得本应用，在遇到问题时请在与卖家约定的服务框架下，优先向卖家求助。如果卖家没有提供您预期的服务，请退款或通过其它形式积极维护您的合法权益。",
            AutoClose = false,
            Severity = InfoBarSeverity.Warning
        });
        WelcomeWindow.WelcomeNavigateForwardCommand.Execute(this);
    }

    private void Intro_OnAnimationEnd(object? sender, EventArgs e)
    {
        ContentRoot.Classes.Add("anim");
    }

    private void ButtonDataMigration_OnClick(object? sender, RoutedEventArgs e)
    {
        var welcomeWindow = TopLevel.GetTopLevel(this) as WelcomeWindow;
        if (welcomeWindow == null)
        {
            return;
        }

        welcomeWindow.Pages.Clear();
        welcomeWindow.Pages.AddRange([typeof(WelcomePage), typeof(LicensePage), typeof(DataTransferPage)]);
        
        NavigateNext();
    }

    private void ButtonEnterRecovery_OnClick(object? sender, RoutedEventArgs e)
    {
        AppBase.Current.Restart(["-m", "-r"]);
    }

    private void ButtonJoinManagement_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new JoinManagementDialog();
        dialog.ShowDialog((TopLevel.GetTopLevel(this) as Window)!);
    }
}