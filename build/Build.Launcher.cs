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
    AbsolutePath LauncherPublishArtifactPath;
    
    Target CleanLauncher => _ => _
        .DependsOn(GenerateMetadata)
        .Executes(() =>
        {
            if (Directory.Exists(LauncherPublishPath))
            {
                Directory.Delete(LauncherPublishPath, true);
            }

            if (File.Exists(LauncherPublishArtifactPath))
            {
                File.Delete(LauncherPublishArtifactPath);
            }
        });
    
    Target CompileLauncher => _ => _
        .DependsOn(GenerateMetadata)
        .DependsOn(CleanLauncher)
        .Executes(() =>
        {
            DotNetPublish(s => s
                .SetProject(LauncherEntryProject)
                .SetConfiguration(Configuration)
                .SetProperty("SelfContained", true)
                .SetProperty("PublishAot", true)
                .SetProperty("TrimMode", "full")
                .SetProperty("PublishBuilding", true)
                .SetProperty("PublishPlatform", Os)
                .SetProperty("RuntimeIdentifier", RuntimeIdentifier)
                .SetProperty("ClassIsland_PlatformTarget", Arch)
                .SetProperty("PublishDir", LauncherPublishPath));
            var extension = Os == "windows" ? ".exe" : "";
            File.Move(LauncherPublishPath / "ClassIsland.Launcher" + extension, LauncherPublishPath / "ClassIsland" + extension);
        });

    Target GenerateLauncherZipArchive => _ => _
        .Produces(LauncherPublishArtifactPath)
        .DependsOn(CompileLauncher)
        .Executes(() =>
        {
            LauncherPublishPath.ZipTo(LauncherPublishArtifactPath);
        });

    Target PublishLauncher => _ => _
        .DependsOn(CompileLauncher)
        .DependsOn(GenerateLauncherZipArchive);
}