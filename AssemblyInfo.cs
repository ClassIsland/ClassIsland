using System.Reflection;
using System.Runtime.Versioning;

[assembly: AssemblyVersion(ThisAssembly.Git.BaseTag)]
[assembly: AssemblyInformationalVersion($"{ThisAssembly.Git.BaseTag}+{ThisAssembly.Git.Sha}")]
[assembly: AssemblyTitle("ClassIsland")]
[assembly: AssemblyProduct("ClassIsland")]
#if NETCOREAPP
[assembly: SupportedOSPlatform("Windows")]
#endif