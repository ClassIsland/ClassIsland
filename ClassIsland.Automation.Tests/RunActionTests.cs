using System.Reflection;
using System.Text.Json;
using ClassIsland.Models.Actions;
using ClassIsland.Platforms.Abstraction;
using ClassIsland.Platforms.Abstraction.Services;
using ClassIsland.Services.Automation.Actions;
using ClassIsland.Shared.Models.Automation;
using Xunit;
using RunType = ClassIsland.Models.Actions.RunActionSettings.RunActionRunType;

namespace ClassIsland.Automation.Tests;

public sealed class RunActionTests : IDisposable
{
    private readonly ILauncherService _previousLauncher = PlatformServices.LauncherService;
    private readonly RecordingLauncher _launcher = new();
    private static readonly PropertyInfo LauncherProperty =
        typeof(PlatformServices).GetProperty(nameof(PlatformServices.LauncherService))!;

    public RunActionTests()
    {
        LauncherProperty.SetValue(null, _launcher);
    }

    [Theory]
    [InlineData(RunType.File, "_classisland-imported:item/document.pdf", "file")]
    [InlineData(RunType.Folder, "_classisland-imported:item/folder", "folder")]
    [InlineData(RunType.AppLink, "exampleapp://document/42", "app")]
    public async Task Invoke_UsesMatchingPlatformOperation(
        RunActionSettings.RunActionRunType type, string value, string operation)
    {
        var item = CreateItem(type, value);
        await InvokeAsync(item);

        Assert.Equal((operation, value), _launcher.Request);
        Assert.Null(item.Exception);
        Assert.True(item.IsCompleted);
        Assert.False(item.IsWorking);
    }

    [Theory]
    [InlineData(RunType.File)]
    [InlineData(RunType.Folder)]
    [InlineData(RunType.AppLink)]
    public async Task Invoke_RecordsPlatformFailureOnAction(RunActionSettings.RunActionRunType type)
    {
        var failure = new InvalidOperationException("System could not open the requested content.");
        _launcher.Failure = failure;
        var item = CreateItem(type, "unavailable");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeAsync(item));

        Assert.Same(failure, actual);
        Assert.Contains(failure.Message, item.Exception);
        Assert.True(item.IsCompleted);
        Assert.False(item.IsWorking);
    }

    [Theory]
    [InlineData("Application", RunType.Application, 0)]
    [InlineData("Command", RunType.Command, 1)]
    [InlineData("File", RunType.File, 2)]
    [InlineData("Folder", RunType.Folder, 3)]
    [InlineData("Url", RunType.Url, 4)]
    [InlineData("AppLink", RunType.AppLink, 5)]
    public void Settings_PreserveSerializedActionTypes(
        string name, RunActionSettings.RunActionRunType type, int numericValue)
    {
        var settings = JsonSerializer.Deserialize<RunActionSettings>(
            $$"""{"RunType":"{{name}}","Value":"original","Args":"--test"}""")!;

        Assert.Equal(type, settings.RunType);
        Assert.Equal(numericValue, (int)settings.RunType);
        Assert.Equal("original", settings.Value);
        Assert.Equal("--test", settings.Args);
        Assert.Contains($"\"RunType\":\"{name}\"", JsonSerializer.Serialize(settings));
    }

    private static ActionItem CreateItem(RunActionSettings.RunActionRunType type, string value) => new()
    {
        Id = "classisland.os.run",
        Settings = new RunActionSettings { RunType = type, Value = value }
    };

    private static async Task InvokeAsync(ActionItem item)
    {
        var set = new ActionSet { ActionItems = [item] };
        set.SetStartRunning(true);
        try
        {
            await new RunAction(null!).InvokeAsync(item, set);
        }
        finally
        {
            set.SetEndRunning(true);
        }
    }

    public void Dispose() => LauncherProperty.SetValue(null, _previousLauncher);

    private sealed class RecordingLauncher : ILauncherService
    {
        public (string Operation, string Value)? Request { get; private set; }
        public Exception? Failure { get; set; }

        public Task LaunchPath(string path) => Record("folder", path);
        public Task LaunchFile(string path) => Record("file", path);
        public Task LaunchUrl(string url) => Record("url", url);
        public Task LaunchAppLink(string url) => Record("app", url);

        private Task Record(string operation, string value)
        {
            Request = (operation, value);
            return Failure == null ? Task.CompletedTask : Task.FromException(Failure);
        }
    }
}
