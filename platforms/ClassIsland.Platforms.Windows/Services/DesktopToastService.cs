using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Windows.UI.Notifications;
using Avalonia.Platform;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Platforms.Abstraction.Models;
using ClassIsland.Platforms.Abstraction.Services;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using SoundFlow.Enums;
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;

namespace ClassIsland.Platform.Windows.Services;

public class DesktopToastService : IDesktopToastService
{
    private string AumId { get; }

    private Dictionary<string, Action> ActivationActions { get; } = new();
    
    public static void RegisterAumidInRegistry(string appId, string displayName, string iconPath)
    {
        // 1. 设置当前进程的 AUMID
        SetCurrentProcessExplicitAppUserModelID(appId);

        // 2. 构造注册表路径
        var regPath = $@"Software\Classes\AppUserModelId\{appId}";

        // 3. 在 HKCU 下创建或打开该键
        using var key = Registry.CurrentUser.CreateSubKey(regPath);
        if (key == null)
            throw new InvalidOperationException("无法创建或打开注册表键：" + regPath);
        
        key.SetValue("DisplayName", displayName, RegistryValueKind.String);
        key.SetValue("IconUri", iconPath, RegistryValueKind.String);
        key.SetValue("IconBackgroundColor", "FFDDDDDD", RegistryValueKind.String);
    }
    
    public DesktopToastService()
    {
        var appPathHash = SHA256.HashData(Encoding.UTF8.GetBytes(AppBase.ExecutingEntrance));
        var base64 = Convert.ToHexString(appPathHash);
        AumId = $"cn.classisland.app+{base64}";
        RegisterAumidInRegistry(AumId, "ClassIsland", Path.Combine(Environment.CurrentDirectory, "Assets", "AppLogo.png"));
    }
    
    public async Task ShowToastAsync(DesktopToastContent content)
    {
        var toastBuilder = new ToastContentBuilder()
            .AddText(content.Title)
            .AddText(content.Body);
        var heroImageUri = await PrepareToastImageResourceAsync(content.HeroImageUri);
        var inlineImageUri = await PrepareToastImageResourceAsync(content.InlineImageUri);
        var logoImageUri = await PrepareToastImageResourceAsync(content.LogoImageUri);
        if (heroImageUri != null)
        {
            toastBuilder.AddHeroImage(heroImageUri);
        }
        if (inlineImageUri != null)
        {
            toastBuilder.AddInlineImage(inlineImageUri);
        }
        if (logoImageUri != null)
        {
            toastBuilder.AddAppLogoOverride(logoImageUri);
        }

        List<string> registeredActions = [];
        foreach (var (text, action) in content.Buttons)
        {
            var actionId = Guid.NewGuid().ToString();
            registeredActions.Add(actionId);
            ActivationActions[actionId] = action;
            toastBuilder.AddButton(text, ToastActivationType.Foreground, actionId);
        }

        var toastContent = toastBuilder
            .GetToastContent();
        toastContent.ActivationType = ToastActivationType.Foreground;
        var toastXml = new XmlDocument();
        toastXml.LoadXml(toastContent.GetContent());
        var toastManagerFactory = ToastNotificationManager.GetDefault();
        var toast = new ToastNotification(toastXml);
        toast.Activated += (sender, args) => Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (args is ToastActivatedEventArgs e && !string.IsNullOrWhiteSpace(e.Arguments)
                                                  && ActivationActions.TryGetValue(e.Arguments, out var action))
            {
                action();
                CleanUpActions();
                return;
            }
            content.Activated?.Invoke(sender, EventArgs.Empty);
            CleanUpActions();
        });
        toast.ExpiresOnReboot = true;
        toast.Dismissed += (_, _) =>
        {
            CleanUpActions();
        };
        toast.Failed += (_, _) =>
        {
            CleanUpActions();
        };
        
        var notifier = toastManagerFactory.CreateToastNotifier(AumId);
        notifier.Show(toast);

        return;
        
        void CleanUpActions()
        {
            foreach (var action in registeredActions)
            {
                ActivationActions.Remove(action);
            }
        }
    }

    public async Task ShowToastAsync(string title, string body, Action? activated = null)
    {
        await ShowToastAsync(new DesktopToastContent()
        {
            Title = title,
            Body = body,
            Activated = (sender, args) => activated?.Invoke()
        });
    }

    private async Task<Uri?> PrepareToastImageResourceAsync(Uri? sourceUri)
    {
        if (sourceUri == null)
        {
            return null;
        }

        try
        {
            switch (sourceUri.Scheme)
            {
                case "file":
                    return sourceUri;
                case "avares":
                {
                    var stream = AssetLoader.Open(sourceUri);
                    var imagePath = Path.GetTempFileName();
                    await using var fileStream = File.Create(imagePath);
                    await stream.CopyToAsync(fileStream);
                    return new Uri(imagePath);
                }
                case "http" or "https":
                {
                    var imagePath = Path.GetTempFileName();
                    var client = new HttpClient();
                    var stream = await client.GetStreamAsync(sourceUri);
                    await using var fileStream = File.Create(imagePath);
                    await stream.CopyToAsync(fileStream);
                    return new Uri(imagePath);
                }
            }
        }
        catch (Exception)
        {
            return null;
        }
        return null;
    }

    public void ActivateNotificationAction(Guid id)
    {
        
    }
}