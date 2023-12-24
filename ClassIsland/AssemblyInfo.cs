using System.Reflection;
using System.Resources;
using System.Runtime.Versioning;
using System.Windows;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page,
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page,
                                              // app, or any theme specific resource dictionaries)
)]

[assembly: AssemblyVersion("1.2.1.1")]
[assembly: AssemblyFileVersion("1.2.1.1")]
[assembly: AssemblyTitle("ClassIsland")]
[assembly: AssemblyProduct("ClassIsland")]
[assembly: SupportedOSPlatform("Windows")]
[assembly: AssemblyInformationalVersion($"1.2.1.1-{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})")]