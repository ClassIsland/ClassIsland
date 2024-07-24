using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services.Management;
using ClassIsland.Core.Controls.CommonDialog;
using ClassIsland.Shared.Abstraction.Services;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Management;
using ClassIsland.Shared.Models.Profile;
using ClassIsland.Shared.Protobuf.Enum;
using ClassIsland.Helpers;
using ClassIsland.Models;

using MaterialDesignThemes.Wpf;

using Microsoft.Extensions.Logging;

using static ClassIsland.Shared.Helpers.ConfigureFileHelper;

using CommonDialog = ClassIsland.Core.Controls.CommonDialog.CommonDialog;

namespace ClassIsland.Services.Management;

public class ManagementService : IManagementService
{
    static ManagementService()
    {
        
    }

    public static void InitManagement()
    {
    }

    public static ManagementService? Instance { get; private set; }

    public static readonly string ManagementPresetPath = "./ManagementPreset.json";
    public static readonly string ManagementConfigureFolderPath =
        Path.Combine(App.AppDataFolderPath, "Management");

    public static readonly string ManagementPersistConfigPath =
        Path.Combine(ManagementConfigureFolderPath, "Persist.json");


    public static readonly string ManagementManifestPath = Path.Combine(ManagementConfigureFolderPath, "Manifest.json");
    public static readonly string ManagementVersionsPath = Path.Combine(ManagementConfigureFolderPath, "Versions.json");
    public static readonly string ManagementSettingsPath = Path.Combine(ManagementConfigureFolderPath, "Settings.json");
    public static readonly string ManagementPolicyPath = Path.Combine(ManagementConfigureFolderPath, "Policy.json");

    public bool IsManagementEnabled { get; set; }

    public ManagementVersions Versions { get; set; } = new();

    public ManagementManifest Manifest { get; set; } = new();

    public ManagementSettings Settings { get; }

    public ManagementPolicy Policy { get; set; } = new();

    public ManagementClientPersistConfig Persist { get;}

    private ILogger<ManagementService> Logger { get; }

    public IManagementServerConnection? Connection { get; }

    public ManagementService(ILogger<ManagementService> logger)
    {
        Instance = this;
        Logger = logger;
        Persist = LoadConfig<ManagementClientPersistConfig>(ManagementPersistConfigPath);
        Settings = LoadConfig<ManagementSettings>(ManagementSettingsPath);
#if DEBUG
        if (App.ApplicationCommand.DisableManagement)
        {
            Logger.LogInformation("集控已被命令行禁用。");
            return;
        }
#endif
        IsManagementEnabled = Settings.IsManagementEnabled;
        if (!IsManagementEnabled)
            return;

        // 连接集控服务器
        try
        {
            switch (Settings.ManagementServerKind)
            {
                case ManagementServerKind.Serverless:
                    Connection = new ServerlessConnection(Persist.ClientUniqueId, Settings.ClassIdentity ?? "", Settings.ManifestUrlTemplate);
                    break;
                case ManagementServerKind.ManagementServer:
                    Connection = new ManagementServerConnection(Settings, Persist.ClientUniqueId, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("", "无效的集控服务器类型。");
            }
            Connection.CommandReceived += ConnectionOnCommandReceived;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "连接集控服务器失败。");
        }
    }

    private void ConnectionOnCommandReceived(object? sender, ClientCommandEventArgs e)
    {
        if (e.Type == CommandTypes.RestartApp)
        {
            AppBase.Current.Restart(true);
        }
    }

    public async Task SetupManagement()
    {
        if (!IsManagementEnabled)
            return;
        
        Logger.LogInformation("正在初始化集控");
        // 读取集控清单
        Manifest = LoadConfig<ManagementManifest>(ManagementManifestPath);
        Policy = LoadConfig<ManagementPolicy>(ManagementPolicyPath);
        Versions = LoadConfig<ManagementVersions>(ManagementVersionsPath);

        if (Connection == null)
        {
            return;
        }

        try
        {
            // 拉取集控清单
            Manifest = await Connection.GetManifest();
            SaveConfig(ManagementManifestPath, Manifest);
            // 拉取策略
            if (Manifest.PolicySource.IsNewerAndNotNull(Versions.PolicyVersion))
            {
                Policy = await Connection.SaveJsonAsync<ManagementPolicy>(Manifest.PolicySource.Value!, ManagementPolicyPath);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "拉取集控清单与策略失败");
        }

    }

    public void SaveSettings()
    {
        Logger.LogInformation("保存集控配置");
        SaveConfig(ManagementVersionsPath, Versions);
    }

    public async Task JoinManagementAsync(ManagementSettings settings)
    {
        if (IsManagementEnabled)
            throw new Exception("无法在已加入管理后再次加入管理。");
        var mf = new ManagementManifest();
        switch (settings.ManagementServerKind)
        {
            case ManagementServerKind.Serverless:
                mf = await WebRequestHelper.GetJson<ManagementManifest>(new Uri(settings.ManifestUrlTemplate));
                break;
            case ManagementServerKind.ManagementServer:
                var connection = new ManagementServerConnection(settings, Persist.ClientUniqueId, true);
                mf = await connection.RegisterAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(settings.ManagementServerKind), "无效的服务器类型。");
        }

        var r = CommonDialog.ShowDialog("ClassIsland", $"确定要加入组织 {mf.OrganizationName} 的管理吗？", new BitmapImage(new Uri("/Assets/HoYoStickers/帕姆_注意.png", UriKind.Relative)),
            60, 60, [
                new DialogAction()
                {
                    PackIconKind = PackIconKind.Cancel,
                    Name = "取消"
                },
                new DialogAction()
                {
                    PackIconKind = PackIconKind.Check,
                    Name = "加入",
                    IsPrimary = true
                }
            ]);
        if (r != 1)
            return;
        var w = CopyObject(settings);
        w.IsManagementEnabled = true;
        // 清空旧的配置
        foreach (var i in new List<string>([ManagementManifestPath, ManagementPolicyPath, ManagementVersionsPath, ProfileService.ManagementClassPlanPath, ProfileService.ManagementSubjectsPath, ProfileService.ManagementTimeLayoutPath, "./Profiles/_management-profile.json"]).Where(File.Exists))
        {
            File.Delete(i);
            if (File.Exists(i + ".bak"))
            {
                File.Delete(i + ".bak");
            }
        }
        SaveConfig(ManagementSettingsPath, w);
        CommonDialog.ShowInfo($"已加入组织 {mf.OrganizationName} 的管理。应用将重启以应用更改。");
        
        AppBase.Current.Restart();
    }

    public async Task ExitManagementAsync()
    {
        if (!IsManagementEnabled)
            throw new Exception("无法在没有加入集控的情况下退出集控。");
        if (!Policy.AllowExitManagement)
            throw new Exception("您的组织不允许您退出集控。");

        var r = CommonDialog.ShowDialog("ClassIsland", $"确定要退出组织 {Manifest.OrganizationName} 的管理吗？", new BitmapImage(new Uri("/Assets/HoYoStickers/帕姆_注意.png", UriKind.Relative)),
            60, 60, [
                new DialogAction()
                {
                    PackIconKind = PackIconKind.Cancel,
                    Name = "取消"
                },
                new DialogAction()
                {
                    PackIconKind = PackIconKind.ExitRun,
                    Name = "退出",
                    IsPrimary = true
                }
            ]);
        if (r != 1) return;
        Settings.IsManagementEnabled = false;
        SaveConfig(ManagementSettingsPath, Settings);

        CommonDialog.ShowInfo($"已退出组织 {Manifest.OrganizationName} 的管理。应用将重启以应用更改。");

        AppBase.Current.Restart();
    }
}