using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls.ProfileTransferProviders;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Extensions.UI;
using ClassIsland.Core.Helpers.UI;
using ClassIsland.Helpers.ProfileTransferHelpers;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Services;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging;

namespace ClassIsland.Controls.ProfileTransferProviders;

public class ClassWidgets2ImportProvider : GenericImportProviderBase
{
    private IProfileService ProfileService { get; } = IAppHost.GetService<IProfileService>();
    private SettingsService SettingsService { get; } = IAppHost.GetService<SettingsService>();
    private ILogger<ClassWidgets2ImportProvider> Logger { get; } =
        IAppHost.GetService<ILogger<ClassWidgets2ImportProvider>>();

    public ClassWidgets2ImportProvider()
    {
        ImportFileHeader = "Class Widgets 2 课表文件路径";
        FileTypes =
        [
            new FilePickerFileType("Class Widgets 2 课表文件")
            {
                Patterns = ["*.json"]
            }
        ];
    }

    public override async Task<bool> InvokeTransfer()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this) ?? AppBase.Current.GetRootWindow();
            using var file = await PlatformServices.FilePickerService.GetFileAsync(SourceFilePath, topLevel)
                             ?? throw new FileNotFoundException("无法打开所选 Class Widgets 2 课表文件。", SourceFilePath);
            await using var stream = await file.OpenReadAsync();
            var analysis = ClassWidgets2ProfileTransferHelper.Analyze(stream);

            string? targetPath = null;
            if (ImportType == 1)
            {
                if (string.IsNullOrWhiteSpace(NewProfileName))
                {
                    throw new InvalidOperationException("请输入新档案名称。");
                }
                targetPath = Path.Combine(Services.ProfileService.ProfilePath, NewProfileName + ".json");
                if (File.Exists(targetPath))
                {
                    throw new InvalidOperationException($"无法导入课表：{targetPath} 已存在。");
                }
            }

            if (analysis.Warnings.Count > 0 && !await ConfirmWarnings(topLevel, analysis))
            {
                return false;
            }

            var profile = ClassWidgets2ProfileTransferHelper.Convert(analysis,
                ImportType == 0 ? ProfileService.Profile : null);
            if (targetPath != null)
            {
                ConfigureFileHelper.SaveConfig(targetPath, profile);
            }

            SynchronizeWeekSettings(analysis);
            if (analysis.Warnings.Count > 0)
            {
                Logger.LogWarning("导入 Class Widgets 2 课表时忽略了不兼容内容：{Summary}", analysis.WarningSummary);
            }

            var settingsMessage = analysis.HasOversizedCycle
                ? "已同步开学日期；因轮换周期超过 9 周，保留了当前多周轮换设置。"
                : "已同步开学日期和多周轮换设置。";
            var ignoredMessage = analysis.IgnoredItemCount > 0
                ? $"已忽略 {analysis.IgnoredItemCount} 项不兼容内容。"
                : "";
            this.ShowSuccessToast($"导入成功。{settingsMessage}{ignoredMessage}");
            return true;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "导入 Class Widgets 2 课表失败");
            this.ShowErrorToast("无法导入 Class Widgets 2 课表", exception);
            return false;
        }
    }

    private static async Task<bool> ConfirmWarnings(TopLevel topLevel, Cw2ImportAnalysis analysis)
    {
        var content = new StackPanel
        {
            Spacing = 8,
            MaxWidth = 560,
            Children =
            {
                new TextBlock
                {
                    Text = "课表中包含 ClassIsland 无法兼容的内容。继续后将忽略以下内容并导入其余部分：",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                new ScrollViewer
                {
                    MaxHeight = 320,
                    Content = new TextBlock
                    {
                        Text = analysis.WarningSummary,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    }
                }
            }
        };
        var result = await new FAContentDialog
        {
            Title = "发现可能无法兼容的课表内容",
            Content = content,
            PrimaryButtonText = "继续导入",
            SecondaryButtonText = "取消",
            DefaultButton = FAContentDialogButton.Primary
        }.ShowAsyncAuto(topLevel);
        return result == FAContentDialogResult.Primary;
    }

    private void SynchronizeWeekSettings(Cw2ImportAnalysis analysis)
    {
        SettingsService.Settings.SingleWeekStartTime = analysis.StartDate.ToDateTime(TimeOnly.MinValue);
        if (analysis.HasOversizedCycle)
        {
            return;
        }

        var maxCycle = Math.Max(2, analysis.MaxWeekCycle);
        SettingsService.Settings.MultiWeekRotationMaxCycle = maxCycle;
        SettingsService.Settings.MultiWeekRotationOffset = new ObservableCollection<int>(
            [-1, -1, .. Enumerable.Repeat(0, maxCycle - 1)]);
    }
}
