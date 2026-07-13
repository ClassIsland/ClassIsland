using System;
using System.Collections.Generic;
using System.IO;
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
    
    [PathVariable] readonly Tool Git;
    
    [Parameter("Arch")] readonly string Arch;
    [Parameter("OsName")] readonly string OsName;
    [Parameter("Package")] readonly string Package;
    [Parameter("BuildType")] readonly string BuildType;
    [Parameter("BuildName")] readonly string BuildName;
    [Parameter("API_SIGNING_KEY")] readonly string ApiSigningKey;
    [Parameter("API_SIGNING_KEY_PS")] readonly string ApiSigningKeyPs;
    [Parameter] readonly string AppVersion;
    [Parameter] readonly string BuildNumber;
    [Parameter] readonly string BrandType;
    [Parameter] readonly string CodesignKey;
    [Parameter] readonly string CodesignProvision;
    [Parameter] readonly string ClassIslandLiveActivityCodesignProvision;
    [Parameter] readonly string ClassIslandDevelopmentTeam;
    [Parameter("Whether the iOS IPA should be code signed. Disable only when producing an unsigned artifact for external signing.")]
    readonly bool EnableCodeSigning = true;
    
    string PublishArtifactName;

    readonly AbsolutePath DesktopAppEntryProject = RootDirectory / "ClassIsland.Desktop" / "ClassIsland.Desktop.csproj";
    readonly AbsolutePath IosAppEntryProject = RootDirectory / "ClassIsland.iOS" / "ClassIsland.iOS.csproj";
    readonly AbsolutePath LauncherEntryProject = RootDirectory / "ClassIsland.Launcher" / "ClassIsland.Launcher.csproj";
    readonly AbsolutePath PluginDevAppPath = RootDirectory / "out" / "ClassIsland_Dev";
    readonly AbsolutePath PluginDevAppPublishPath = RootDirectory / "out" / "ClassIsland_Dev" / "bin";
    readonly AbsolutePath NupkgEntryProject = RootDirectory / "ClassIsland.Filter.Linux.slnf";
    readonly AbsolutePath AppOutputPath = RootDirectory / "out";
    readonly AbsolutePath AppPublishPath = RootDirectory / "out" / "ClassIsland";
    readonly AbsolutePath LauncherPublishPath = RootDirectory / "out" / "Launcher";
    readonly AbsolutePath AppSecretsPath = RootDirectory / "ClassIsland" / "secrets.g.cs";

    bool IsIosBuild => OsName == "ios";

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = Configuration.Release ;
    
    Version GitVersion;
    int GitCommitCount;
    
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
                "macos" => "osx",
                "ios" => "ios",
                _ => throw new InvalidOperationException($"不支持的平台：{OsName}")
            };
            RuntimeIdentifier = $"{osRid}-{Arch}";
            PublishArtifactName = $"out_{BuildName}_{OsName}_{Arch}_{BuildType}_{Package}";
            IsSecretFilled = !(string.IsNullOrEmpty(ApiSigningKey) || string.IsNullOrEmpty(ApiSigningKeyPs));
            AppPublishArtifactPath = AppOutputPath / PublishArtifactName + ".zip";
            IosPublishArtifactPath = AppOutputPath / PublishArtifactName + ".ipa";
            LauncherPublishArtifactPath = AppOutputPath / PublishArtifactName + ".zip";

            if (IsIosBuild)
            {
                if (Package != "ipa" || Arch != "arm64")
                {
                    throw new InvalidOperationException("iOS 发布仅支持 --Package ipa --Arch arm64。");
                }

                var missingBuildParameter = new[]
                {
                    AppVersion,
                    BuildNumber,
                    BrandType
                }.Any(string.IsNullOrWhiteSpace);
                if (missingBuildParameter)
                {
                    throw new InvalidOperationException("iOS IPA publishing requires appVersion, buildNumber, and brandType.");
                }

                var missingSigningParameter = EnableCodeSigning && new[]
                {
                    CodesignKey,
                    CodesignProvision,
                    ClassIslandLiveActivityCodesignProvision,
                    ClassIslandDevelopmentTeam
                }.Any(string.IsNullOrWhiteSpace);
                if (missingSigningParameter)
                {
                    throw new InvalidOperationException("Signed iOS IPA publishing requires signing parameters for both the app and Live Activity extension.");
                }
            }
            
            Log.Information("AppVersion = {AppVersion}", AppVersion);
            Log.Information("RuntimeIdentifier = {RuntimeIdentifier}", RuntimeIdentifier);
            Log.Information("EnableCodeSigning = {EnableCodeSigning}", EnableCodeSigning);
            Log.Information("IsSecretFilled = {IsSecretFilled}", IsSecretFilled);
            Log.Information("PublishArtifactName = {PublishArtifactName}", PublishArtifactName);
            Log.Information("AppPublishArtifactPath = {AppPublishArtifactPath}", AppPublishArtifactPath);
            Log.Information("IosPublishArtifactPath = {IosPublishArtifactPath}", IosPublishArtifactPath);
            Log.Information("LauncherPublishArtifactPath = {LauncherPublishArtifactPath}", LauncherPublishArtifactPath);
        });
    
    Target PopulateGitVersion => _ => _
        .Executes(() =>
        {
            var gitVersion = GitVersion = Version.TryParse(Git("describe --tags --abbrev=0").StdToText() ?? "0.0.0.0",
                out var v)
                ? v
                : new Version(0, 0, 0, 0);
            var gitCommitCount = GitCommitCount = int.TryParse(Git("rev-list --count HEAD").StdToText(), out var count)
                ? count
                : 0;
            Log.Information("GitVersion = {gitVersion}", gitVersion);
            Log.Information("GitCommitCount = {gitCommitCount}", gitCommitCount);
        });

    Target CleanOutputDir => _ => _
        .Executes(() =>
        {
            if (!Directory.Exists(AppOutputPath))
            {
                return;
            }
            foreach (var dir in Directory.EnumerateDirectories(AppOutputPath))
            {
                if (Path.GetFullPath(dir) == PluginDevAppPath)
                {
                    continue;
                }

                Directory.Delete(dir, true);
            }

            foreach (var file in Directory.EnumerateFiles(AppOutputPath))
            {
                File.Delete(file);
            }
        });
    
    static string PlatformExecutableExtension => OperatingSystem.IsWindows() ? ".exe" : "";
}
