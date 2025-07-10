using System;
using System.IO;
using Microsoft.Build.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    string RuntimeIdentifier = "";
    AbsolutePath AppPublishArtifactPath;
    bool IsSecretFilled = false;
    
    Target CleanDesktopApp => _ => _
        .Before(Restore)
        .Before(CompileApp)
        .Executes(() =>
        {
            DotNetClean(s => s.SetProject(DesktopAppEntryProject));
            if (Directory.Exists(AppOutputPath))
            {
                Directory.Delete(AppOutputPath, true);
            }
        });

    Target GenerateMetadata => _ => _
        .Executes(() =>
        {
            var osRid = Os switch
            {
                "windows" => "win",
                "linux" => "linux",
                _ => throw new InvalidOperationException($"不支持的平台：{Os}")
            };
            RuntimeIdentifier = $"{osRid}-{Arch}";

            IsSecretFilled = !(string.IsNullOrEmpty(ApiSigningKey) || string.IsNullOrEmpty(ApiSigningKeyPs));
            AppPublishArtifactPath = AppOutputPath /
                                     (string.IsNullOrWhiteSpace(PublishArtifactName)
                                         ? "ClassIsland.zip"
                                         : PublishArtifactName);
            
            Log.Information("RuntimeIdentifier = {RuntimeIdentifier}", this);
            Log.Information("IsSecretFilled = {IsSecretFilled}", this);
            Log.Information("AppPublishArtifactPath = {AppPublishArtifactPath}", this);
        });

    Target Restore => _ => _
        .DependsOn(GenerateMetadata)
        .DependsOn(CleanDesktopApp)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProperty("RuntimeIdentifier", RuntimeIdentifier)
                .SetProjectFile(Solution));
        });

    Target GenerateSecrets => t => t
        .Executes(() =>
        {
            var content = 
               $$""""
                 namespace ClassIsland.Services.SpeechService{
                     public static partial class GptSovitsSecrets
                     {
                         public const string PrivateKey = 
                 """
                 {{ApiSigningKey}}
                 """;
                     
                         public const string PrivateKeyPassPhrase = 
                 """
                 {{ApiSigningKeyPs}}
                 """;
                     
                         public const bool IsSecretsFilled = {{IsSecretFilled.ToString().ToLower()}};
                     }
                 }
                 """";
            File.WriteAllText(AppSecretsPath, content);
        });
    
    Target CompileApp => t => t
        .DependsOn(Restore)
        .DependsOn(GenerateSecrets)
        .DependsOn(GenerateMetadata)
        .Requires(() => Os)
        .Requires(() => Arch)
        .Executes(() =>
        {
            DotNetPublish(s => s
                .SetProject(DesktopAppEntryProject)
                .SetConfiguration(Configuration)
                .SetProperty("PublishBuilding", true)
                .SetProperty("PublishPlatform", Os)
                .SetProperty("RuntimeIdentifier", RuntimeIdentifier)
                .SetProperty("ClassIsland_PlatformTarget", Arch)
                .SetProperty("PublishDir", AppPublishPath)
                .EnableNoRestore());
        });

    Target GenerateAppZipArchive => _ => _
        .Produces(AppPublishArtifactPath)
        .DependsOn(CompileApp)
        .Executes(() =>
        {
            AppPublishPath.ZipTo(AppPublishArtifactPath);
        });

    Target PostCleanup => _ => _
        .After(CompileApp)
        .DependsOn(GenerateSecrets)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            if (File.Exists(AppSecretsPath))
            {
                File.Delete(AppSecretsPath);
            }
        });

    Target PublishApp => _ => _
        .DependsOn(CompileApp)
        .DependsOn(GenerateAppZipArchive)
        .DependsOn(PostCleanup);
}