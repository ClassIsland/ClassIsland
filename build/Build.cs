using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.CompileApp);
    
    [Solution] readonly Solution Solution;
    
    [Parameter("Arch")] readonly string Arch;
    [Parameter("OsName")] readonly string OsName;
    [Parameter("Package")] readonly string Package;
    [Parameter("BuildType")] readonly string BuildType;
    [Parameter("BuildName")] readonly string BuildName;
    [Parameter("API_SIGNING_KEY")] readonly string ApiSigningKey;
    [Parameter("API_SIGNING_KEY_PS")] readonly string ApiSigningKeyPs;
    [Parameter] readonly string AppVersion;
    
    string PublishArtifactName;

    readonly AbsolutePath DesktopAppEntryProject = RootDirectory / "ClassIsland.Desktop" / "ClassIsland.Desktop.csproj";
    readonly AbsolutePath LauncherEntryProject = RootDirectory / "ClassIsland.Launcher" / "ClassIsland.Launcher.csproj";
    readonly AbsolutePath AppOutputPath = RootDirectory / "out";
    readonly AbsolutePath AppPublishPath = RootDirectory / "out" / "ClassIsland";
    readonly AbsolutePath LauncherPublishPath = RootDirectory / "out" / "Launcher";
    readonly AbsolutePath AppSecretsPath = RootDirectory / "ClassIsland" / "secrets.g.cs";

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    

    Target GenerateMetadata => _ => _
        .Requires(() => OsName)
        .Requires(() => Arch)
        .Requires(() => Package)
        .Requires(() => BuildType)
        .Requires(() => BuildName)
        .Executes(() =>
        {
            var osRid = OsName switch
            {
                "windows" => "win",
                "linux" => "linux",
                _ => throw new InvalidOperationException($"不支持的平台：{OsName}")
            };
            RuntimeIdentifier = $"{osRid}-{Arch}";
            PublishArtifactName = $"out_{BuildName}_{OsName}_{Arch}_{BuildType}_{Package}";
            IsSecretFilled = !(string.IsNullOrEmpty(ApiSigningKey) || string.IsNullOrEmpty(ApiSigningKeyPs));
            AppPublishArtifactPath = AppOutputPath / PublishArtifactName + ".zip";
            LauncherPublishArtifactPath = AppOutputPath / PublishArtifactName + ".zip";
            
            Log.Information("RuntimeIdentifier = {RuntimeIdentifier}", RuntimeIdentifier);
            Log.Information("IsSecretFilled = {IsSecretFilled}", IsSecretFilled);
            Log.Information("PublishArtifactName = {PublishArtifactName}", PublishArtifactName);
            Log.Information("AppPublishArtifactPath = {AppPublishArtifactPath}", AppPublishArtifactPath);
            Log.Information("LauncherPublishArtifactPath = {LauncherPublishArtifactPath}", LauncherPublishArtifactPath);
        });
}
