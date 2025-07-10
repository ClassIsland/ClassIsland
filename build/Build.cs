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
    [Parameter("Os")] readonly string Os;
    [Parameter("API_SIGNING_KEY")] readonly string ApiSigningKey;
    [Parameter("API_SIGNING_KEY_PS")] readonly string ApiSigningKeyPs;
    [Parameter("artifact_name")] readonly string PublishArtifactName;

    readonly AbsolutePath DesktopAppEntryProject = RootDirectory / "ClassIsland.Desktop" / "ClassIsland.Desktop.csproj";
    readonly AbsolutePath DesktopAppLauncherEntryProject = RootDirectory / "ClassIsland.Launcher" / "ClassIsland.Launcher.csproj";
    readonly AbsolutePath AppOutputPath = RootDirectory / "out";
    readonly AbsolutePath AppPublishPath = RootDirectory / "out" / "ClassIsland";
    readonly AbsolutePath AppSecretsPath = RootDirectory / "ClassIsland" / "secrets.g.cs";

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    

}
