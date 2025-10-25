using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using ClassIsland.Core.Controls;
using FluentAvalonia.UI.Controls;

namespace ClassIsland.Views.WelcomePages;

public partial class LicensePage : UserControl
{
    public LicensePage()
    {
        InitializeComponent();
    }

    private async void ButtonShowOssLicense_OnClick(object? sender, RoutedEventArgs e)
    {
        var license = await new StreamReader(AssetLoader.Open(new Uri("avares://ClassIsland/Assets/LICENSE.txt")))
            .ReadToEndAsync();
        await new ContentDialog()
        {
            Title = "开放源代码许可",
            Content = new TextBlock()
            {
                Text = license
            },
            PrimaryButtonText = "关闭",
            DefaultButton = ContentDialogButton.Primary
        }.ShowAsync();
    }

    private void ButtonShowPrivacyLicense_OnClick(object? sender, RoutedEventArgs e)
    {
        var mdReader = new DocumentReaderWindow()
        {
            Source = new Uri("avares://ClassIsland/Assets/Documents/Privacy_.md"),
            Title = "隐私政策"
        }.ShowDialog((TopLevel.GetTopLevel(this) as Window)!);
    }
}