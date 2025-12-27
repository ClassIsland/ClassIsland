using System.Reflection;
using ClassIsland;

#if NIX
[assembly: AssemblyVersion("0.0.0.0")]
[assembly: AssemblyInformationalVersion("NIXBUILD+NIXBUILD_LONG_HASH")]
#endif
[assembly: AssemblyVersion(GitInfo.Tag)]
[assembly: AssemblyInformationalVersion($"{GitInfo.Tag}+{GitInfo.CommitHash}")]

[assembly: AssemblyTitle("ClassIsland")]
[assembly: AssemblyProduct("ClassIsland")]
#if NETCOREAPP
// [assembly: SupportedOSPlatform("Windows")]
#endif
#if Platforms_MacOs
[assembly:SupportedOSPlatform("macos")]
#endif
 