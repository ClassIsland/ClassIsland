using System.Reflection;
using System.Runtime.Versioning;

#if NIX
[assembly: AssemblyVersion("0.0.0.0")]
[assembly: AssemblyInformationalVersion("NIXBUILD+NIXBUILD_LONG_HASH")]
#else
#if DEBUG
[assembly: AssemblyVersion("2.0.0.999")]
#else
[assembly: AssemblyVersion(ThisAssembly.Git.BaseTag)]
#endif
[assembly: AssemblyInformationalVersion($"{ThisAssembly.Git.BaseTag}+{ThisAssembly.Git.Sha}")]
#endif
[assembly: AssemblyTitle("ClassIsland")]
[assembly: AssemblyProduct("ClassIsland")]
#if NETCOREAPP
// [assembly: SupportedOSPlatform("Windows")]
#endif
#if Platforms_MacOs
[assembly:SupportedOSPlatform("macos")]
#endif
 