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
    [Parameter("IsProductionBuild")] readonly bool IsProductionBuild;

    
    Target RestoreAndroidApp => _ => _
        .Before(CompileAndroidApp)
        .DependsOn(GenerateMetadata)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(AndroidAppEntryProject)
                .SetProperty("PublishBuilding", true)
                .SetProperty("PublishPlatform", OsName)
                .SetProperty("RuntimeIdentifier", RuntimeIdentifier)
                .SetProperty("ClassIsland_PlatformTarget", Arch));
        });
    
    Target CleanAndroidApp => _ => _
        .Before(CompileAndroidApp)
        .DependsOn(CleanOutputDir)
        .DependsOn(GenerateMetadata)
        .DependsOn(RestoreAndroidApp)
        .Executes(() =>
        {
            DotNetClean(s => s
                .SetProject(AndroidAppEntryProject)
                .SetProperty("PublishBuilding", true)
                .SetProperty("PublishPlatform", OsName)
                .SetProperty("RuntimeIdentifier", RuntimeIdentifier)
                .SetProperty("ClassIsland_PlatformTarget", Arch));
        });

    Target CompileAndroidApp => t => t
        .DependsOn(GenerateSecrets)
        .DependsOn(GenerateMetadata)
        .DependsOn(PopulateGitVersion)
        .DependsOn(CleanAndroidApp)
        .Executes(() =>
        {
            DotNetPublish(s => s
                .SetProject(AndroidAppEntryProject)
                .SetConfiguration(Configuration)
                .SetProperty("PublishBuilding", true)
                .SetProperty("PublishPlatform", OsName)
                .SetProperty("RuntimeIdentifier", RuntimeIdentifier)
                .SetProperty("ClassIsland_PlatformTarget", Arch)
                .SetProperty("ClassIsland_MonoAoT", true)
                .SetProperty("PublishDir", AppPublishPath)
                .SetProperty("BrandType", IsProductionBuild ? "Production" : "Beta")
                .SetProperty("ApplicationVersion", Math.Max(GitCommitCount, 1))
                .SetProperty("ApplicationDisplayVersion", AppVersion));
            if (Package == "apk")
            {
                var apkPath = Directory.GetFiles(AppPublishPath, "*.apk")
                    .OrderByDescending(x => Path.GetFileNameWithoutExtension(x).EndsWith("-Signed", StringComparison.OrdinalIgnoreCase))
                    .First();
                File.Move(apkPath, AppOutputPath / PublishArtifactName + ".apk", true);
            }
        });

    Target PublishAndroidApp => _ => _
        .DependsOn(CompileAndroidApp)
        .DependsOn(PostCleanup);
}
