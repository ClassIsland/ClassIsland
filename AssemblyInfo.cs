using System.Reflection;
using System.Runtime.Versioning;

#if NIX
[assembly: AssemblyVersion("0.0.0.0")]
[assembly: AssemblyInformationalVersion("NIXBUILD+NIXBUILD_LONG_HASH")]
#else
[assembly: AssemblyVersion(ThisAssembly.Git.BaseTag)]
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
