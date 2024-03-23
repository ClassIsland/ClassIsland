using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using ClassIsland.Controls;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Helpers;
using ClassIsland.Core.Models.Management;
using ClassIsland.Helpers;
using ClassIsland.Models;
using MaterialDesignThemes.Wpf;
using static ClassIsland.Core.Helpers.ConfigureFileHelper;
using Application = System.Windows.Application;
using CommonDialog = ClassIsland.Controls.CommonDialog;

namespace ClassIsland.Services;

public class ManagementService
{
    static ManagementService()
    {
        
    }

    public static void InitManagement()
    {
        Instance = new ManagementService();
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

    public ManagementService()
    {
        Persist = LoadConfig<ManagementClientPersistConfig>(ManagementPersistConfigPath);
        Settings = LoadConfig<ManagementSettings>(ManagementSettingsPath);
        IsManagementEnabled = Settings.IsManagementEnabled;
        if (!IsManagementEnabled)
            return;

        SetupManagement();
    }

    private async void SetupManagement()
    {
        // 读取集控清单
        Manifest = LoadConfig<ManagementManifest>(ManagementManifestPath);
        Policy = LoadConfig<ManagementPolicy>(ManagementPolicyPath);
        Versions = LoadConfig<ManagementVersions>(ManagementVersionsPath);

        try
        {
            // 拉取集控清单
            Manifest = await WebRequestHelper.SaveJson<ManagementManifest>(new Uri(Settings.ManifestUrlTemplate), ManagementManifestPath);
            // 拉取策略
            if (Manifest.PolicySource.IsNewerAndNotNull(Versions.PolicyVersion))
            {
                Policy = await WebRequestHelper.SaveJson<ManagementPolicy>(new Uri(Manifest.PolicySource.Value!), ManagementPolicyPath);
            }
        }
        catch (Exception e)
        {
            // ignored
        }

    }

    public void SaveSettings()
    {
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
                // TODO: 从集控服务器获取
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
        SaveConfig(ManagementSettingsPath, w);
        CommonDialog.ShowInfo($"已加入组织 {mf.OrganizationName} 的管理。应用将重启以应用更改。");

        App.ReleaseLock();
        Application.Current.Shutdown();
        System.Windows.Forms.Application.Restart();
    }
}