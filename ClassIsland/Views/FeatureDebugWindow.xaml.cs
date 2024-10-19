using System;
using System.Threading.Tasks;
using System.Windows;

using ClassIsland.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Helpers;
using ClassIsland.Models.Authorize;

namespace ClassIsland.Views;

/// <summary>
/// FeatureDebugWindow.xaml 的交互逻辑
/// </summary>
public partial class FeatureDebugWindow : MyWindow
{
    public ILessonsService LessonsService { get; }

    public IProfileService ProfileService { get; }
    public IAuthorizeService AuthorizeService { get; }

    public FeatureDebugWindow(ILessonsService lessonsService, IProfileService profileService, IAuthorizeService authorizeService)
    {
        DataContext = this;
        LessonsService = lessonsService;
        ProfileService = profileService;
        AuthorizeService = authorizeService;
        InitializeComponent();
    }

    private void ButtonPlayEffect_OnClick(object sender, RoutedEventArgs e)
    {
        RippleEffect.Play();
    }

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        
    }

    private async void ButtonTestFakeLoading_OnClick(object sender, RoutedEventArgs e)
    {
        LoadingMask.StartFakeLoading();
        await Task.Delay(TimeSpan.FromSeconds(5));
        LoadingMask.FinishFakeLoading();

    }

    private void ButtonShowAuthWindow_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new AuthorizeWindow(new Credential(), true);
        window.ShowDialog();
    }

    private async void ButtonCreateCredential_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new CommonDialogBuilder().HasInput(true)
            .AddConfirmAction()
            .SetContent("输入原先的认证字符串")
            .ShowDialog(out var credentialString, this);
        credentialString = await AuthorizeService.SetupCredentialStringAsync(string.IsNullOrEmpty(credentialString) ? null : credentialString);
        CommonDialog.ShowInfo(credentialString ?? "");
        Clipboard.SetDataObject(credentialString ?? "", false);
    }

    private async void ButtonAuthorize_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new CommonDialogBuilder().HasInput(true)
            .AddConfirmAction()
            .SetContent("输入认证字符串")
            .ShowDialog(out var credentialString, this);
        var result = await AuthorizeService.AuthorizeAsync(credentialString);
        CommonDialog.ShowInfo(result.ToString());
    }
}