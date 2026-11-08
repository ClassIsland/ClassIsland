using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Platform;
using ClassIsland.Core.Extensions;
using ClassIsland.Models.Profile;
using ClassIsland.Shared.Models.Profile;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Services;

public partial class ProfileService
{
    private async Task<Profile> DownloadManagementProfileAsync(string url)
    {
        using var document = await ManagementService.Connection!.GetJsonAsync<JsonDocument>(url);
        return ReadProfileJson(document.RootElement.GetRawText());
    }

    internal static Profile ReadProfile(string path)
    {
        try
        {
            return ReadProfileJson(File.ReadAllText(path));
        }
        catch (ProfileLoadException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ProfileReadException(exception);
        }
    }

    private static Profile ReadProfileJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new ProfileLoadException("档案必须是 JSON 对象。");

        // 在解析业务数据之前检查兼容性，避免新版本的数据类型触发旧版本的恢复写入。
        var migrationProperties = root.EnumerateObject().Where(x => x.NameEquals(nameof(Profile.Migrations))).ToList();
        if (migrationProperties.Count > 1)
            throw new ProfileLoadException("档案包含重复的迁移列表。");
        List<ProfileMigration> migrations = [];
        if (migrationProperties.Count == 1)
        {
            try
            {
                migrations = migrationProperties[0].Value.Deserialize<List<ProfileMigration>>()
                             ?? throw new ProfileLoadException("档案迁移列表不能为空值。");
            }
            catch (JsonException exception)
            {
                throw new ProfileLoadException("档案迁移记录无效。", exception);
            }
        }
        CheckMigrationCompatibility(migrations);
        return root.Deserialize<Profile>() ?? throw new ProfileLoadException("档案内容为空。");
    }

    private static void ValidateMigrationRecords(IEnumerable<ProfileMigration> migrations)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var migration in migrations)
        {
            if (migration == null || string.IsNullOrWhiteSpace(migration.Id) || !ids.Add(migration.Id))
                throw new ProfileLoadException("迁移记录包含空标识或重复标识。");
        }
    }

    private static void CheckMigrationCompatibility(List<ProfileMigration> migrations)
    {
        ValidateMigrationRecords(CurrentMigrations);
        ValidateMigrationRecords(migrations);
        var currentIds = CurrentMigrations.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var migration in migrations)
        {
            if (!currentIds.Contains(migration.Id) && !migration.AllowDowngrade)
                throw new ProfileDowngradeException(migration.Id);
        }
    }

    private async Task ApplyMigrationsAsync(Profile profile)
    {
        CheckMigrationCompatibility(profile.Migrations);
        var currentIds = CurrentMigrations.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        profile.Migrations.RemoveAll(x => !currentIds.Contains(x.Id) && x.RemoveOnDowngrade);
        var appliedIds = profile.Migrations.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var migration in CurrentMigrations)
        {
            if (appliedIds.Contains(migration.Id))
                continue;

            Logger.LogInformation("正在应用档案迁移：{MigrationId}", migration.Id);
            try
            {
                await ApplyMigrationAsync(profile, migration.Id);
            }
            catch (Exception exception)
            {
                throw new ProfileLoadException($"档案迁移失败：{migration.Id}", exception);
            }
            profile.Migrations.Add(new ProfileMigration
            {
                Id = migration.Id,
                AllowDowngrade = migration.AllowDowngrade,
                RemoveOnDowngrade = migration.RemoveOnDowngrade
            });
        }
    }

    private static Task ApplyMigrationAsync(Profile profile, string migrationId)
    {
        // 新迁移必须在此显式实现；仅登记元数据不应被误认为已经完成迁移。
        return migrationId switch
        {
            "classisland.subjects.extension1" => ApplySubjectExtensionMigration(profile),
            _ => throw new InvalidOperationException($"档案迁移尚未实现：{migrationId}")
        };
    }

    private static Task ApplySubjectExtensionMigration(Profile profile)
    {
        var json = AssetLoader.ReadAllText(new Uri("avares://ClassIsland/Assets/ProfileTemplates/default.json"));
        var template = JsonSerializer.Deserialize<Profile>(json)
                       ?? throw new InvalidOperationException("无法读取默认档案模板。");
        ApplySubjectExtensionMigration(profile, template);
        return Task.CompletedTask;
    }

    private static void ApplySubjectExtensionMigration(Profile profile, Profile template)
    {
        foreach (var (id, subject) in profile.Subjects)
        {
            if (!template.Subjects.TryGetValue(id, out var templateSubject))
                continue;

            subject.Icon = templateSubject.Icon;
            subject.ColorHex = templateSubject.ColorHex;
            subject.Location = templateSubject.Location;
        }
    }

    private static void CommitLoadedProfile(string path, Profile profile, bool backupOriginal)
    {
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write))
            {
                JsonSerializer.Serialize(stream, profile);
                stream.Flush(true);
            }
            if (backupOriginal)
                File.Copy(path, path + ".bak", true);
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
