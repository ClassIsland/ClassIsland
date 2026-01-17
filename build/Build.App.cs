using System;
using System.IO;
using System.Linq;
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
        .Before(CompileApp)
        .DependsOn(GenerateMetadata)
        .Executes(() =>
        {
            DotNetClean(s => s
                .SetProject(DesktopAppEntryProject)
                .SetProperty("PublishBuilding", true)
                .SetProperty("PublishPlatform", OsName)
                .SetProperty("RuntimeIdentifier", RuntimeIdentifier)
                .SetProperty("ClassIsland_PlatformTarget", Arch));
            if (Directory.Exists(AppOutputPath))
            {
                Directory.Delete(AppOutputPath, true);
            }
            if (File.Exists(AppPublishArtifactPath))
            {
                File.Delete(AppPublishArtifactPath);
            }
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
        .DependsOn(GenerateSecrets)
        .DependsOn(GenerateMetadata)
        .DependsOn(CleanDesktopApp)
        .Executes(() =>
        {
            var createDeb = Package == "deb";
            var isSelfContained = BuildType == "selfContained";
            DotNetPublish(s => s
                .SetProject(DesktopAppEntryProject)
                .SetConfiguration(Configuration)
                .SetProperty("PublishBuilding", true)
                .SetProperty("PublishPlatform", OsName)
                .SetProperty("RuntimeIdentifier", RuntimeIdentifier)
                .SetProperty("ClassIsland_PlatformTarget", Arch)
                .SetProperty("SelfContained", isSelfContained)
                .SetProperty("ClassIsland_SelfContained", isSelfContained)
                .SetProperty("PublishDir", Package == "pkg" ? AppOutputPath : AppPublishPath)
                .SetProperty("DebUOSOutputFilePath", AppOutputPath / PublishArtifactName + ".deb")
                .SetProperty("UOSDebVersion", AppVersion)
                .SetProperty("ApplicationVersion", AppVersion)
                .SetProperty("ApplicationDisplayVersion", AppVersion)
                .SetProperty("AutoCreateDebUOSAfterPublish", createDeb));
            if (Package == "pkg")
            {
                File.Move(Directory.GetFiles(AppOutputPath).First(x => Path.GetExtension(x) == ".pkg"),
                    AppOutputPath / PublishArtifactName + ".pkg");
            }
        });

    Target GenerateAppZipArchive => _ => _
        .Produces(AppPublishArtifactPath)
        .DependsOn(CompileApp)
        .OnlyWhenDynamic(() => Package != "deb" && Package != "pkg")
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