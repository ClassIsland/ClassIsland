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
    
    Target PostCleanup => _ => _
        .After(CompileApp)
        .After(CompileAndroidApp)
        .DependsOn(GenerateSecrets)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            if (File.Exists(AppSecretsPath))
            {
                File.Delete(AppSecretsPath);
            }
        });
    
}
