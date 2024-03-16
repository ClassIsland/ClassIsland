using System;
using System.IO;
using System.Net;
using ClassIsland.Core.Models.Management;
using ClassIsland.Helpers;
using static ClassIsland.Core.Helpers.ConfigureFileHelper;

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
}