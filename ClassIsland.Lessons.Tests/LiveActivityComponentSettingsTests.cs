using System.Text.Json;
using ClassIsland.Core;
using ClassIsland.Models;
using ClassIsland.Models.LiveActivities;
using ClassIsland.Services;
using ClassIsland.ViewModels.SettingsPages;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClassIsland.Lessons.Tests;

// 保存测试会暂时替换全局配置目录，不能与其他测试并行执行。
[CollectionDefinition(nameof(LiveActivitySettingsCollection), DisableParallelization = true)]
public sealed class LiveActivitySettingsCollection;

[Collection(nameof(LiveActivitySettingsCollection))]
public sealed class LiveActivityComponentSettingsTests
{
    [Fact]
    public void JsonRoundTrip_WithEmptyLayout_PreservesTheEmptyLayout()
    {
        var settings = new Settings
        {
            IsLiveActivityLayoutCustomized = true,
            LiveActivityComponents = []
        };

        var restored = RoundTrip(settings);

        Assert.True(restored.IsLiveActivityLayoutCustomized);
        Assert.Empty(restored.LiveActivityComponents);
    }

    [Fact]
    public void JsonRoundTrip_PreservesDisabledComponentsAndCustomSettings()
    {
        var target = new DateTime(2027, 6, 7, 8, 30, 0);
        var settings = new Settings
        {
            IsLiveActivityLayoutCustomized = true,
            LiveActivityComponents =
            [
                new()
                {
                    Kind = LiveActivityComponentKind.Text,
                    IsEnabled = false,
                    Text = "记得带计算器"
                },
                new()
                {
                    Kind = LiveActivityComponentKind.Countdown,
                    UseCustomCountdown = true,
                    CountdownName = "距离考试",
                    TargetDate = target
                },
                new()
                {
                    Kind = LiveActivityComponentKind.Schedule,
                    ShowNextLesson = false,
                    ShowProgress = false
                }
            ]
        };

        var restored = RoundTrip(settings);

        Assert.True(restored.IsLiveActivityLayoutCustomized);
        Assert.Collection(restored.LiveActivityComponents,
            component =>
            {
                Assert.Equal(LiveActivityComponentKind.Text, component.Kind);
                Assert.False(component.IsEnabled);
                Assert.Equal("记得带计算器", component.Text);
            },
            component =>
            {
                Assert.Equal(LiveActivityComponentKind.Countdown, component.Kind);
                Assert.True(component.UseCustomCountdown);
                Assert.Equal("距离考试", component.CountdownName);
                Assert.Equal(target, component.TargetDate);
            },
            component =>
            {
                Assert.Equal(LiveActivityComponentKind.Schedule, component.Kind);
                Assert.False(component.ShowNextLesson);
                Assert.False(component.ShowProgress);
            });
    }

    [Fact]
    public void JsonRead_WithUnknownKindsAndNullEntries_PreservesOnlyValidComponents()
    {
        var settings = JsonSerializer.Deserialize<Settings>("""
            {
              "IsLiveActivityLayoutCustomized": true,
              "LiveActivityComponents": [
                null,
                { "Kind": 999, "Text": "unknown" },
                { "Kind": 5, "IsEnabled": false, "Text": "保留的文本" },
                { "Kind": 4, "UseCustomCountdown": true, "TargetDate": null }
              ]
            }
            """)!;

        var restored = RoundTrip(settings);

        Assert.Collection(restored.LiveActivityComponents,
            component =>
            {
                Assert.Equal(LiveActivityComponentKind.Text, component.Kind);
                Assert.False(component.IsEnabled);
                Assert.Equal("保留的文本", component.Text);
            },
            component =>
            {
                Assert.Equal(LiveActivityComponentKind.Countdown, component.Kind);
                Assert.True(component.UseCustomCountdown);
                Assert.Null(component.TargetDate);
            });
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"LiveActivityComponents\": null}")]
    public void JsonRead_WithMissingOrNullLayout_UsesDefaults(string json)
    {
        var restored = JsonSerializer.Deserialize<Settings>(json)!;

        Assert.Equal(LiveActivityComponentSettings.CreateDefault().Select(x => x.Kind),
            restored.LiveActivityComponents.Select(x => x.Kind));
        Assert.False(restored.IsLiveActivityLayoutCustomized);
    }

    [Fact]
    public void Normalize_FiltersInvalidEntriesAndLimitsTheValidComponentCount()
    {
        var components = new List<LiveActivityComponentSettings>
        {
            null!,
            new() { Kind = (LiveActivityComponentKind)999 }
        };
        components.AddRange(Enumerable.Range(0, LiveActivityComponentSettings.MaximumComponents + 3)
            .Select(index => new LiveActivityComponentSettings
            {
                Kind = LiveActivityComponentKind.Text,
                Text = index.ToString()
            }));

        var normalized = LiveActivityComponentSettings.Normalize(components);

        Assert.Equal(LiveActivityComponentSettings.MaximumComponents, normalized.Count);
        Assert.Equal(Enumerable.Range(0, LiveActivityComponentSettings.MaximumComponents)
            .Select(index => index.ToString()), normalized.Select(x => x.Text));
        Assert.Empty(LiveActivityComponentSettings.Normalize([]));
    }

    [Fact]
    public void DefaultLayouts_AreIndependentAndNotifyChildChanges()
    {
        var settings = new Settings();
        var anotherSettings = new Settings();
        var changes = RecordComponentChanges(settings);

        settings.LiveActivityComponents[0].IsEnabled = false;

        Assert.Single(changes);
        Assert.True(anotherSettings.LiveActivityComponents[0].IsEnabled);
        Assert.NotSame(settings.LiveActivityComponents[0], anotherSettings.LiveActivityComponents[0]);
    }

    [Fact]
    public void ComponentChanges_NotifyTheParentSettings()
    {
        var component = new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Countdown };
        var settings = new Settings { LiveActivityComponents = [component] };
        var changes = RecordComponentChanges(settings);

        component.Text = "新的文本";
        component.IsEnabled = false;
        component.UseCustomCountdown = true;
        component.CountdownName = "距离考试";
        component.TargetDate = new DateTime(2027, 6, 7);
        component.ShowNextLesson = false;
        component.ShowProgress = false;

        Assert.Equal(7, changes.Count);
    }

    [Fact]
    public void CollectionChanges_NotifyTheParentSettings()
    {
        var settings = new Settings { LiveActivityComponents = [] };
        var changes = RecordComponentChanges(settings);
        var text = new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Text };
        var date = new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Date };

        settings.LiveActivityComponents.Add(text);
        settings.LiveActivityComponents.Add(date);
        settings.LiveActivityComponents.Move(0, 1);
        settings.LiveActivityComponents.Remove(text);

        Assert.Equal(4, changes.Count);
        Assert.Same(date, Assert.Single(settings.LiveActivityComponents));
    }

    [Fact]
    public void RemovedOrReplacedComponents_StopNotifyingTheParentSettings()
    {
        var removed = new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Text };
        var replaced = new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Text };
        var settings = new Settings { LiveActivityComponents = [removed, replaced] };
        var changes = RecordComponentChanges(settings);
        var oldCollection = settings.LiveActivityComponents;

        oldCollection.Remove(removed);
        changes.Clear();
        removed.Text = "已经移除";
        Assert.Empty(changes);

        var replacement = new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Text };
        settings.LiveActivityComponents = [replacement];
        changes.Clear();
        replaced.Text = "旧方案";
        oldCollection.Add(removed);
        removed.IsEnabled = false;
        Assert.Empty(changes);

        replacement.Text = "新方案";
        Assert.Single(changes);
    }

    [Fact]
    public void Commands_PreserveOrderAndIgnoreMovementOutsideTheLayout()
    {
        var viewModel = CreateViewModel();
        viewModel.SettingsService.Settings.LiveActivityComponents = [];
        viewModel.AddComponentCommand.Execute(LiveActivityComponentKind.Text);
        viewModel.AddComponentCommand.Execute(LiveActivityComponentKind.Date);
        viewModel.AddComponentCommand.Execute(LiveActivityComponentKind.Clock);
        var text = viewModel.Components[0];
        var date = viewModel.Components[1];
        var clock = viewModel.Components[2];
        var unknown = new LiveActivityComponentSettings();

        viewModel.MoveUpCommand.Execute(text);
        viewModel.MoveDownCommand.Execute(clock);
        viewModel.MoveUpCommand.Execute(unknown);
        viewModel.MoveDownCommand.Execute(unknown);
        Assert.Equal(new[] { text, date, clock }, viewModel.Components);

        viewModel.MoveDownCommand.Execute(text);
        Assert.Equal(new[] { date, text, clock }, viewModel.Components);
        viewModel.MoveUpCommand.Execute(clock);
        Assert.Equal(new[] { date, clock, text }, viewModel.Components);

        viewModel.RemoveComponentCommand.Execute(unknown);
        viewModel.RemoveComponentCommand.Execute(clock);
        Assert.Equal(new[] { date, text }, viewModel.Components);
    }

    [Fact]
    public void AddCommand_RejectsUnknownKindsAndStopsAtTheComponentLimit()
    {
        var viewModel = CreateViewModel();
        viewModel.SettingsService.Settings.LiveActivityComponents = [];

        viewModel.AddComponentCommand.Execute((LiveActivityComponentKind)999);
        Assert.Empty(viewModel.Components);
        Assert.True(viewModel.AddComponentCommand.CanExecute(LiveActivityComponentKind.Text));

        for (var index = 0; index < LiveActivityComponentSettings.MaximumComponents + 2; index++)
        {
            viewModel.AddComponentCommand.Execute(LiveActivityComponentKind.Text);
        }

        Assert.Equal(LiveActivityComponentSettings.MaximumComponents, viewModel.Components.Count);
        Assert.False(viewModel.CanAddComponent);
        Assert.False(viewModel.AddComponentCommand.CanExecute(LiveActivityComponentKind.Text));

        viewModel.RemoveComponentCommand.Execute(viewModel.Components[0]);
        Assert.True(viewModel.CanAddComponent);
        Assert.True(viewModel.AddComponentCommand.CanExecute(LiveActivityComponentKind.Text));
    }

    [Fact]
    public void RemoveLastComponent_KeepsTheEmptyLayoutAndRestoreDefaultsRestoresTheDefaultOrder()
    {
        var viewModel = CreateViewModel();
        var component = new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Text };
        viewModel.SettingsService.Settings.LiveActivityComponents = [component];

        viewModel.RemoveComponentCommand.Execute(component);
        Assert.Empty(viewModel.Components);

        viewModel.RestoreDefaultsCommand.Execute(null);

        Assert.Equal(LiveActivityComponentSettings.CreateDefault().Select(x => x.Kind),
            viewModel.Components.Select(x => x.Kind));
    }

    [Fact]
    public void ActivateDeactivate_RepeatedCallsDoNotDuplicateNotificationsAndSaveActiveChanges()
    {
        using var environment = new SettingsTestEnvironment();
        var viewModel = CreateViewModel();
        var component = new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Text };
        viewModel.SettingsService.Settings.LiveActivityComponents = [component];
        var changes = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

        try
        {
            viewModel.Activate();
            viewModel.Activate();
            changes.Clear();
            component.Text = "已保存";

            Assert.Single(changes.Where(x => x == nameof(LiveActivitySettingsViewModel.Components)));
            Assert.Single(changes.Where(x => x == nameof(LiveActivitySettingsViewModel.CanAddComponent)));
            var saved = JsonSerializer.Deserialize<Settings>(File.ReadAllText(environment.SettingsPath))!;
            Assert.Equal("已保存", Assert.Single(saved.LiveActivityComponents).Text);

            viewModel.Deactivate();
            viewModel.Deactivate();
            changes.Clear();
            component.Text = "页面已关闭";
            Assert.Empty(changes);
            saved = JsonSerializer.Deserialize<Settings>(File.ReadAllText(environment.SettingsPath))!;
            Assert.Equal("已保存", Assert.Single(saved.LiveActivityComponents).Text);

            viewModel.Activate();
            changes.Clear();
            component.Text = "再次保存";
            Assert.Single(changes.Where(x => x == nameof(LiveActivitySettingsViewModel.Components)));
            saved = JsonSerializer.Deserialize<Settings>(File.ReadAllText(environment.SettingsPath))!;
            Assert.Equal("再次保存", Assert.Single(saved.LiveActivityComponents).Text);
        }
        finally
        {
            viewModel.Deactivate();
        }
    }

    [Fact]
    public void ActiveViewModel_WhenSettingsAreReplaced_UnsubscribesTheOldSettings()
    {
        using var environment = new SettingsTestEnvironment();
        var viewModel = CreateViewModel();
        var oldSettings = viewModel.SettingsService.Settings;
        var replacementComponent = new LiveActivityComponentSettings { Kind = LiveActivityComponentKind.Text };
        var replacement = new Settings { LiveActivityComponents = [replacementComponent] };
        var changes = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

        try
        {
            viewModel.Activate();
            viewModel.SettingsService.Settings = replacement;
            Assert.Same(replacement.LiveActivityComponents, viewModel.Components);
            changes.Clear();

            oldSettings.LiveActivityComponents[0].IsEnabled = false;
            Assert.Empty(changes);

            replacementComponent.Text = "替换后的配置";
            Assert.Single(changes.Where(x => x == nameof(LiveActivitySettingsViewModel.Components)));
            var saved = JsonSerializer.Deserialize<Settings>(File.ReadAllText(environment.SettingsPath))!;
            Assert.Equal("替换后的配置", Assert.Single(saved.LiveActivityComponents).Text);

            viewModel.Deactivate();
            changes.Clear();
            viewModel.SettingsService.Settings = new Settings();
            replacementComponent.Text = "停用后修改";
            Assert.Empty(changes);
        }
        finally
        {
            viewModel.Deactivate();
        }
    }

    private static Settings RoundTrip(Settings settings) =>
        JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(settings))!;

    private static List<string?> RecordComponentChanges(Settings settings)
    {
        var changes = new List<string?>();
        settings.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Settings.LiveActivityComponents))
            {
                changes.Add(args.PropertyName);
            }
        };
        return changes;
    }

    private static LiveActivitySettingsViewModel CreateViewModel() =>
        new(new SettingsService(NullLogger<SettingsService>.Instance, null!));

    private sealed class SettingsTestEnvironment : IDisposable
    {
        private readonly string _previousRoot = CommonDirectories.AppRootFolderPath;
        private string Root { get; } = Path.Combine(Path.GetTempPath(), $"classisland-live-activity-{Guid.NewGuid():N}");
        public string SettingsPath => Path.Combine(Root, "Settings.json");

        public SettingsTestEnvironment()
        {
            Directory.CreateDirectory(Root);
            typeof(CommonDirectories).GetProperty(nameof(CommonDirectories.AppRootFolderPath))!.SetValue(null, Root);
        }

        public void Dispose()
        {
            typeof(CommonDirectories).GetProperty(nameof(CommonDirectories.AppRootFolderPath))!.SetValue(null, _previousRoot);
            Directory.Delete(Root, true);
        }
    }
}
