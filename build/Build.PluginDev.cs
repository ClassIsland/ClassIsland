using System;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    readonly AbsolutePath LinuxUserProfile = AbsolutePath.Create(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    readonly AbsolutePath MacOsProfile = AbsolutePath.Create(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) / ".zprofile";
    
    Target RestorePluginDevDesktopApp => _ => _
        .Before(CompilePluginDevDesktopApp)
        .Before(CleanPluginDevDesktopApp)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(DesktopAppEntryProject)
                .SetPlatform("Any CPU"));
        });
    
    Target CleanPluginDevDesktopApp => _ => _
        .DependsOn(RestorePluginDevDesktopApp)
        .DependsOn(CleanOutputDir)
        .Executes(() =>
        {
            DotNetClean(s => s
                .SetProject(DesktopAppEntryProject)
                .SetProperty("PublishDir", PluginDevAppPath));
            if (Directory.Exists(PluginDevAppPublishPath))
            {
                Directory.Delete(PluginDevAppPublishPath, true);
            }
        });
    
    Target CompilePluginDevDesktopApp => _ => _
        .DependsOn(RestorePluginDevDesktopApp)
        .DependsOn(CleanPluginDevDesktopApp)
        .DependsOn(PopulateGitVersion)
        .Executes(() =>
        {
            DotNetPublish(s => s.SetProject(DesktopAppEntryProject)
                .SetConfiguration(Configuration.Debug)
                .SetProperty("Platform", "Any CPU")
                .SetProperty("Version", GitVersion)
                .SetProperty("NuGetVersion", GitVersion)
                .SetProperty("PublishDir", PluginDevAppPublishPath)
                .SetVerbosity(DotNetVerbosity.quiet));
            File.WriteAllText(PluginDevAppPath / "PackageType", "folder");
        });

    Target SetupPluginDevEnvEnvironmentVariables => _ => _
        .After(CompilePluginDevDesktopApp)
        .Executes(() =>
        {
            Environment.SetEnvironmentVariable("ClassIsland_DebugBinaryFile", PluginDevAppPublishPath / "ClassIsland.Desktop" + PlatformExecutableExtension, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable("ClassIsland_DebugBinaryDirectory", PluginDevAppPublishPath, EnvironmentVariableTarget.User);

            if (OperatingSystem.IsLinux())
            {
                UpdateEnvProfile(LinuxUserProfile / ".bashrc");
                UpdateEnvProfile(LinuxUserProfile / ".zshrc");
                UpdateEnvProfile(LinuxUserProfile / ".profile");
                UpdateEnvProfile(LinuxUserProfile / ".bash_profile");
                UpdateEnvProfile(LinuxUserProfile / ".bash_login");
                UpdateEnvProfile(LinuxUserProfile / ".xprofile");
            }
            
            Log.Warning("⚠️环境变量已设置，重启计算机以生效。");
        });
    
    Target InitPluginDevEnv => _ => _
        .DependsOn(CompilePluginDevDesktopApp)
        .DependsOn(SetupPluginDevEnvEnvironmentVariables)
        .Executes(() =>
        {
            
        });

    void UpdateEnvProfile(string profilePath)
    {
        var text = File.Exists(profilePath)
            ? File.ReadAllText(profilePath)
            : string.Empty;
        var profileBlock = 
            $"""
            #BEGIN_ClassIsland_Dev_EnvVars
            # 此部分内容由 ClassIsland 插件开发环境初始化脚本自动添加
            export ClassIsland_DebugBinaryFile="{PluginDevAppPublishPath / "ClassIsland.Desktop" + PlatformExecutableExtension}"
            export ClassIsland_DebugBinaryDirectory="{PluginDevAppPublishPath}"
            #END_ClassIsland_Dev_EnvVars
            """;
        var blockRegex = BlockRegex();
        
        string newText;
        if (!blockRegex.IsMatch(text))
        {
            // 不存在 → 追加
            newText = string.IsNullOrWhiteSpace(text)
                ? profileBlock + "\n"
                : text.TrimEnd() + "\n\n" + profileBlock + "\n";
        }
        else
        {
            var existing = blockRegex.Match(text).Value;

            if (existing == profileBlock.TrimEnd())
            {
                // 完全一致 → 不做任何事
                return;
            }

            // 存在但不同 → 整块替换
            newText = blockRegex.Replace(text, profileBlock);
        }

        if (profilePath != null) File.WriteAllText(profilePath, newText);
    }

    [GeneratedRegex(@"#BEGIN_ClassIsland_Dev_EnvVars[\s\S]*?#END_ClassIsland_Dev_EnvVars", RegexOptions.Multiline)]
    private static partial Regex BlockRegex();
}