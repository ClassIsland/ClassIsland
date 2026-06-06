using System.Reflection;
using System.Runtime.Versioning;
using ClassIsland;

#if NIX
[assembly: AssemblyVersion("0.0.0.0")]
[assembly: AssemblyInformationalVersion("NIXBUILD+NIXBUILD_LONG_HASH")]
#else
[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyInformationalVersion("2.0.0.0+dev")]
#endif

[assembly: AssemblyTitle("ClassIsland")]
[assembly: AssemblyProduct("ClassIsland")]
#if NETCOREAPP
// [assembly: SupportedOSPlatform("Windows")]
#endif
#if Platforms_MacOs
[assembly:SupportedOSPlatform("macos")]
#endif
 
