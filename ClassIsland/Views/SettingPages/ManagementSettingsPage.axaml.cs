using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ClassIsland.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.ViewModels.SettingsPages;
using FluentAvalonia.UI.Controls;
using Net.Codecrete.QrCodeGenerator;

namespace ClassIsland.Views.SettingPages;

[SettingsPageInfo("management", "集控", true, SettingsPageCategory.About)]
public partial class ManagementSettingsPage : SettingsPageBase
{
    public IManagementService ManagementService { get; }
    public ManagementSettingsViewModel ViewModel { get; } = new();

    public ManagementSettingsPage(IManagementService managementService)
    {
        ManagementService = managementService;
        DataContext = this;
        InitializeComponent();
    }

    private void ButtonJoinManagement_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new JoinManagementDialog();
        dialog.ShowDialog((TopLevel.GetTopLevel(this) as Window)!);
    }

    private async void ManagementSettingsPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!ManagementService.IsManagementEnabled)
            return;

        var qrcode = QrCode.EncodeText(ManagementService.Persist.ClientUniqueId.ToString(), QrCode.Ecc.Medium);
        ViewModel.CuidQrCodePath = Geometry.Parse(qrcode.ToGraphicsPath());

        await LoadRemoteAssistStatus();
    }

    private async Task LoadRemoteAssistStatus()
    {
        try
        {
            var cuid = ManagementService.Persist.ClientUniqueId;
            var host = ManagementService.Settings.ManagementServer;
            using var client = CreateHttpClient();
            var response = await client.GetStringAsync($"{host}/api/v1/clients/{cuid}/remote-assist/status");
            var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            ViewModel.IsRemoteAssistEnabled = root.GetProperty("enabled").GetBoolean();
            ViewModel.IsPinVisible = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载远程协助状态失败: {ex.Message}");
        }
    }

    private async void EnableRemoteAssistButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new TaskDialog
        {
            Title = "ClassIsland",
            SubHeader = "启用远程协助",
            Content = "启用远程协助后，管理员将可以通过集控服务器向本设备发送远程命令。\n\n此功能存在一定安全风险，请确认您信任所在的管理组织。\n\n启用后将生成一个 6 位 PIN 码，请妥善保管。关闭此功能需要在 WebUI 管理端操作。",
            Buttons =
            {
                TaskDialogButton.CancelButton,
                new TaskDialogButton("启用", true)
                {
                    IsDefault = true
                }
            },
            XamlRoot = TopLevel.GetTopLevel(this) as Window,
        };

        var result = await dialog.ShowAsync();
        if (result?.Equals(true) == true)
        {
            await EnableRemoteAssist();
        }
    }

    private async Task EnableRemoteAssist()
    {
        try
        {
            var cuid = ManagementService.Persist.ClientUniqueId;
            var host = ManagementService.Settings.ManagementServer;
            var pin = GeneratePin();

            using var client = CreateHttpClient();
            var content = new StringContent(
                JsonSerializer.Serialize(new { pin }),
                Encoding.UTF8,
                "application/json");
            var response = await client.PostAsync($"{host}/api/v1/clients/{cuid}/remote-assist/enable", content);
            response.EnsureSuccessStatusCode();

            ViewModel.IsRemoteAssistEnabled = true;
            ViewModel.RemoteAssistPin = pin;
            ViewModel.IsPinVisible = true;

            var pinDialog = new TaskDialog
            {
                Title = "ClassIsland",
                SubHeader = "远程协助已启用",
                Content = $"您的远程协助 PIN 码为：{pin}\n\n请记下此 PIN 码，管理员需要输入此 PIN 才能向本设备发送命令。\n\n关闭远程协助或重置 PIN 需要在 WebUI 管理端操作。",
                Buttons =
                {
                    new TaskDialogButton("确定", true)
                    {
                        IsDefault = true
                    }
                },
                XamlRoot = TopLevel.GetTopLevel(this) as Window,
            };
            await pinDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            ViewModel.IsRemoteAssistEnabled = false;
            System.Diagnostics.Debug.WriteLine($"启用远程协助失败: {ex.Message}");
        }
    }

    private void ShowPinButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPinVisible = !ViewModel.IsPinVisible;
    }

    private static string GeneratePin()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            UseProxy = false,
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            }
        };
        return new HttpClient(handler);
    }
}
