using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Parameter("IsRelease")] readonly bool IsRelease;
    string NupkgVersion;

    Target CleanNupkg => _ => _
        .DependsOn(CleanOutputDir)
        .Executes(() =>
        {
            DotNetClean(s => s
                .SetProject(NupkgEntryProject));
        });


    Target PopulateNupkgVersion => _ => _
        .DependsOn(PopulateGitVersion)
        .Executes(() =>
        {
            var version = GitVersion.ToString();
            if (!IsRelease)
            {
                version += $"-dev{GitCommitCount}";
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