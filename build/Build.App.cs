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
    AbsolutePath IosPublishArtifactPath;
    bool IsSecretFilled = false;
    
    Target RestoreDesktopApp => _ => _
        .Before(CompileApp)
        .DependsOn(GenerateMetadata)
        .Executes(() =>
        {
            if (IsIosBuild)
            {
                DotNetRestore(s => s
                    .SetProjectFile(IosAppEntryProject)
                    .SetProperty("RuntimeIdentifier", RuntimeIdentifier));
                return;
            }

            DotNetRestore(s => s
                .SetProjectFile(DesktopAppEntryProject)
                .SetProperty("PublishBuilding", true)
                .SetProperty("PublishPlatform", OsName)
                .SetProperty("RuntimeIdentifier", RuntimeIdentifier)
                .SetProperty("ClassIsland_PlatformTarget", Arch));
        });
    
    Target CleanDesktopApp => _ => _
        .Before(CompileApp)
        .DependsOn(CleanOutputDir)
        .DependsOn(GenerateMetadata)
        .DependsOn(RestoreDesktopApp)
        .Executes(() =>
        {
            if (IsIosBuild)
            {
                DotNetClean(s => s
                    .SetProject(IosAppEntryProject)
                    .SetProperty("RuntimeIdentifier", RuntimeIdentifier));
                return;
            }

            DotNetClean(s => s
                .SetProject(DesktopAppEntryProject)
                .SetProperty("PublishBuilding", true)
                .SetProperty("PublishPlatform", OsName)
                .SetProperty("RuntimeIdentifier", RuntimeIdentifier)
                .SetProperty("ClassIsland_PlatformTarget", Arch));
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
            if (IsIosBuild)
            {
                DotNetPublish(s => s
                    .SetProject(IosAppEntryProject)
                    .SetConfiguration(Configuration)
                    .SetProperty("RuntimeIdentifier", RuntimeIdentifier)
                    .SetProperty("ArchiveOnBuild", true)
                    .SetProperty("BuildIpa", true)
                    .SetProperty("BrandType", BrandType)
                    .SetProperty("ApplicationDisplayVersion", AppVersion)
                    .SetProperty("ApplicationVersion", BuildNumber)
                    .SetProperty("CodesignKey", CodesignKey)
                    .SetProperty("CodesignProvision", CodesignProvision)
                    .SetProperty("ClassIslandLiveActivityCodesignProvision", ClassIslandLiveActivityCodesignProvision)
                    .SetProperty("ClassIslandDevelopmentTeam", ClassIslandDevelopmentTeam)
                    .SetProperty("IpaPackagePath", IosPublishArtifactPath));
                return;
            }

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
        .OnlyWhenDynamic(() => Package != "deb" && Package != "pkg" && Package != "ipa")
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
