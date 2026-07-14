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

public partial class Build
{
    Target RestoreDesktopApp => _ => _
        .Before(CompileApp)
        .DependsOn(GenerateMetadata)
        .Executes(() =>
        {
            if (IsIosBuild)
            {
                DotNetRestore(s => s
                    .SetProjectFile(IosAppEntryProject)
                    .SetProperty("PublishBuilding", true)
                    .SetProperty("PublishPlatform", OsName)
                    .SetProperty("ClassIsland_PlatformTarget", Arch)
                    .SetProperty("GeneratePackageOnBuild", false)
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
                    .SetProperty("PublishBuilding", true)
                    .SetProperty("PublishPlatform", OsName)
                    .SetProperty("ClassIsland_PlatformTarget", Arch)
                    .SetProperty("GeneratePackageOnBuild", false)
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

    Target CompileApp => t => t
        .DependsOn(GenerateSecrets)
        .DependsOn(GenerateMetadata)
        .DependsOn(CleanDesktopApp)
        .Executes(() =>
        {
            if (IsIosBuild)
            {
                DotNetPublish(settings =>
                {
                    var enableCodeSigning = EnableCodeSigning ? "true" : "false";
                    settings = settings
                        .SetProject(IosAppEntryProject)
                        // iOS 的多层项目引用会以不同全局属性重复构建 Avalonia 项目；
                        // 串行执行可避免它们同时写入同一个 obj/Avalonia/resources 文件。
                        .SetProcessAdditionalArguments("-m:1")
                        .SetConfiguration(Configuration)
                        .SetProperty("PublishBuilding", true)
                        .SetProperty("PublishPlatform", OsName)
                        .SetProperty("ClassIsland_PlatformTarget", Arch)
                        .SetProperty("GeneratePackageOnBuild", false)
                        .SetProperty("WarningsAsErrors", "CA1416")
                        .SetProperty("RuntimeIdentifier", RuntimeIdentifier)
                        .SetProperty("ArchiveOnBuild", enableCodeSigning)
                        .SetProperty("BuildIpa", true)
                        .SetProperty("EnableCodeSigning", enableCodeSigning)
                        .SetProperty("BrandType", BrandType)
                        .SetProperty("ApplicationDisplayVersion", AppVersion)
                        .SetProperty("ApplicationVersion", BuildNumber)
                        .SetProperty("IpaPackagePath", IosPublishArtifactPath);

                    if (string.Equals(
                            (string)Configuration,
                            "Release",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        // Global.props 默认开启符号；通过全局 MSBuild 属性同时约束
                        // iOS 主项目及所有 ProjectReference，避免 PDB 进入发布 IPA。
                        settings = settings
                            .SetProperty("DebugType", "none")
                            .SetProperty("DebugSymbols", false);
                    }

                    if (!EnableCodeSigning)
                    {
                        return settings;
                    }

                    return settings
                        .SetProperty("CodesignKey", CodesignKey)
                        .SetProperty("CodesignProvision", CodesignProvision)
                        .SetProperty("ClassIslandLiveActivityCodesignProvision", ClassIslandLiveActivityCodesignProvision)
                        .SetProperty("ClassIslandDevelopmentTeam", ClassIslandDevelopmentTeam);
                });
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
                .SetProperty("ApplicationVersion", GitCommitCount)
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

    Target PublishApp => _ => _
        .DependsOn(CompileApp)
        .DependsOn(GenerateAppZipArchive)
        .DependsOn(PostCleanup);
}
