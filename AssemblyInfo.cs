using System.Reflection;
using System.Runtime.Versioning;

[assembly: AssemblyVersion(ThisAssembly.Git.BaseTag)]
[assembly: AssemblyTitle("ClassIsland")]
[assembly: AssemblyProduct("ClassIsland")]
#if NETCOREAPP
[assembly: SupportedOSPlatform("Windows")]
#endif