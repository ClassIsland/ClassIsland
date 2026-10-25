using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClassIsland.Core.Models.Automation;
using ClassIsland.Core.Models.Components;
using ClassIsland.Models;
using ClassIsland.Models.Tutorial;
using ClassIsland.Shared.Helpers;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Services;

/// <summary>
/// 在替换在线数据前验证暂存区中由 ClassIsland 管理的配置。
/// 插件自有和未知文件保持不透明。
/// </summary>
internal static class AppDataConfigurationValidator
{
    public static void Validate(
        string stagingRoot,
        bool validateSettings,
        bool validateProfiles,
        bool validateConfig)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingRoot);

        if (validateSettings)
        {
            _ = Deserialize<Settings>(
                Path.Combine(stagingRoot, "Settings.json"),
                JsonValueKind.Object);
        }

        if (validateProfiles)
        {
            ValidateProfiles(Path.Combine(stagingRoot, "Profiles"));
        }

        if (validateConfig)
        {
            ValidateKnownConfigFiles(Path.Combine(stagingRoot, "Config"));
        }
    }

    public static void ValidateAvailable(string stagingRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingRoot);

        var settingsPath = Path.Combine(stagingRoot, "Settings.json");
        if (HasPrimaryOrBackup(settingsPath))
        {
            _ = Deserialize<Settings>(settingsPath, JsonValueKind.Object);
        }

        var profilesPath = Path.Combine(stagingRoot, "Profiles");
        if (Directory.Exists(profilesPath))
        {
            ValidateProfiles(profilesPath);
        }

        ValidateKnownConfigFiles(Path.Combine(stagingRoot, "Config"));
    }

    private static void ValidateKnownConfigFiles(string configRoot)
    {
        if (!Directory.Exists(configRoot))
        {
            return;
        }

        foreach (var path in StagedDataImportValidator.EnumeratePrimaryJsonFiles(
                     Path.Combine(configRoot, "Automations")))
        {
            _ = Deserialize<ObservableCollection<Workflow>>(
                path,
                JsonValueKind.Array,
                (workflows, candidate) =>
                {
                    if (workflows.Any(workflow => workflow == null))
                    {
                        throw InvalidRequiredValue(
                            candidate,
                            "workflow entry");
                    }
                });
        }

        foreach (var path in StagedDataImportValidator.EnumeratePrimaryJsonFiles(
                     Path.Combine(configRoot, "ComponentLayouts")))
        {
            _ = Deserialize<ComponentProfile>(
                path,
                JsonValueKind.Object,
                (components, candidate) =>
                {
                    if (components.Lines == null)
                    {
                        throw InvalidRequiredValue(
                            candidate,
                            nameof(ComponentProfile.Lines));
                    }
                });
        }

        var enabledThemesPath = Path.Combine(configRoot, "EnabledThemes.json");
        if (HasPrimaryOrBackup(enabledThemesPath))
        {
            _ = Deserialize<ObservableCollection<string>>(
                enabledThemesPath,
                JsonValueKind.Array,
                (enabledThemes, candidate) =>
                {
                    if (enabledThemes.Any(themeId => themeId == null))
                    {
                        throw InvalidRequiredValue(candidate, "theme id");
                    }
                });
        }

        var tutorialPath = Path.Combine(configRoot, "Tutorial.json");
        if (HasPrimaryOrBackup(tutorialPath))
        {
            _ = Deserialize<TutorialSettings>(
                tutorialPath,
                JsonValueKind.Object,
                (tutorial, candidate) =>
                {
                    if (tutorial.CompletedTutorials == null)
                    {
                        throw InvalidRequiredValue(
                            candidate,
                            nameof(TutorialSettings.CompletedTutorials));
                    }
                });
        }
    }

    private static void ValidateProfiles(string profilesPath)
    {
        _ = StagedDataImportValidator.ValidateProfileDirectory(
            profilesPath,
            candidate =>
            {
                var profile = DeserializeCandidate<Profile>(
                    candidate,
                    JsonValueKind.Object);
                ValidateProfile(profile, candidate);
            });
    }

    private static void ValidateProfile(Profile profile, string path)
    {
        if (profile.TimeLayouts == null)
        {
            throw InvalidRequiredValue(path, nameof(Profile.TimeLayouts));
        }

        if (profile.ClassPlans == null)
        {
            throw InvalidRequiredValue(path, nameof(Profile.ClassPlans));
        }

        if (profile.Subjects == null)
        {
            throw InvalidRequiredValue(path, nameof(Profile.Subjects));
        }

        if (profile.ClassPlanGroups == null)
        {
            throw InvalidRequiredValue(path, nameof(Profile.ClassPlanGroups));
        }

        if (profile.OrderedSchedules == null)
        {
            throw InvalidRequiredValue(path, nameof(Profile.OrderedSchedules));
        }
    }

    private static T Deserialize<T>(
        string primaryPath,
        JsonValueKind expectedRootKind,
        Action<T, string>? validate = null)
    {
        return StagedDataImportValidator.LoadPrimaryOrBackup(
            primaryPath,
            candidate =>
            {
                var value = DeserializeCandidate<T>(
                    candidate,
                    expectedRootKind);
                validate?.Invoke(value, candidate);
                return value;
            });
    }

    private static T DeserializeCandidate<T>(
        string path,
        JsonValueKind expectedRootKind)
    {
        StagedDataImportValidator.ValidateJsonFile(path, expectedRootKind);
        try
        {
            return ConfigureFileHelper.LoadConfigUnWrapped<T>(path, false);
        }
        catch (Exception exception) when (exception is not InvalidDataException)
        {
            throw new InvalidDataException(
                $"ClassIsland 配置无法按应用数据模型读取：{path}",
                exception);
        }
    }

    private static bool HasPrimaryOrBackup(string primaryPath) =>
        File.Exists(primaryPath) || File.Exists(primaryPath + ".bak");

    private static InvalidDataException InvalidRequiredValue(
        string path,
        string valueName) =>
        new($"ClassIsland 配置缺少必需值 {valueName}：{path}");
}
