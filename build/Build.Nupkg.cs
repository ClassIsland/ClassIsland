using System;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;
using Serilog.Core;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Parameter("IsRelease")] readonly bool IsRelease;
    string NupkgVersion;
    Version GitVersion;
    
    Target CleanNupkg => _ => _
        .Executes(() =>
        {
            DotNetClean(s => s
                .SetProject(NupkgEntryProject));
            if (Directory.Exists(AppOutputPath))
            {
                Directory.Delete(AppOutputPath, true);
            }
        });
    
    
    Target PopulateNupkgVersion => _ => _
        .Executes(() =>
        {
            var gitVersion = GitVersion = Version.TryParse(Git("describe --tags --abbrev=0").StdToText() ?? "0.0.0.0",
                out var v)
                ? v
                : new Version(0, 0, 0, 0);
            var gitCommitCount = int.TryParse(Git("rev-list --count HEAD").StdToText(), out var count)
                ? count
                : 0;
            Log.Information("GitVersion = {gitVersion}", gitVersion);
            Log.Information("GitCommitCount = {gitCommitCount}", gitCommitCount);
            var version = gitVersion.ToString();
            if (!IsRelease)
            {
                version += $"-dev{gitCommitCount}";
            }

            NupkgVersion = version;
            Log.Information("NupkgVersion = {NupkgVersion}", NupkgVersion);
        });

    Target BuildNupkg => _ => _
        .DependsOn(CleanNupkg)
        .DependsOn(PopulateNupkgVersion)
        .Before(CopyNupkg)
        .Executes(() =>
        {
            DotNetBuild(s => s.SetProjectFile(NupkgEntryProject)
                .SetConfiguration(Configuration)
                .SetProperty("Platform", "Any CPU")
                .SetProperty("Version", GitVersion)
                .SetProperty("PackageVersion", NupkgVersion)
                .SetArtifactsPath(AppOutputPath));
        });

    Target CopyNupkg => _ => _
        .DependsOn(BuildNupkg)
        .Executes(() =>
        {
            
        });
}