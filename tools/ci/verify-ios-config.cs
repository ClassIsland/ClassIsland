#:property PublishAot=false
using System.Reflection;
using System.Runtime.Loader;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: verify-ios-config.cs <app-bundle-directory>");
    return 64;
}

var folder = Path.GetFullPath(args[0]);
var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"classisland-config-check-{Guid.NewGuid():N}");
var context = new AssemblyLoadContext("ClassIsland configuration verification", isCollectible: true);
context.Resolving += (_, name) =>
{
    var path = Path.Combine(folder, name.Name + ".dll");
    return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
};

try
{
    // 仅在宿主运行时测试包内的跨平台共享库，数据写入独立临时目录。
    Directory.CreateDirectory(temporaryDirectory);
    var shared = context.LoadFromAssemblyPath(Path.Combine(folder, "ClassIsland.Shared.dll"));
    var configType = shared.GetType("ClassIsland.Shared.Models.Management.ManagementClientPersistConfig", throwOnError: true)!;
    var helper = shared.GetType("ClassIsland.Shared.Helpers.ConfigureFileHelper", throwOnError: true)!;
    var load = helper.GetMethods().Single(method => method.Name == "LoadConfig" &&
        method.IsGenericMethodDefinition && method.GetParameters().Length == 3).MakeGenericMethod(configType);
    var configPath = Path.Combine(temporaryDirectory, "Persist.json");
    var first = load.Invoke(null, [configPath, null, false]);
    var getId = configType.GetProperty("ClientUniqueId")!;
    var id = (Guid)getId.GetValue(first)!;
    if (id == Guid.Empty || !File.Exists(configPath))
        throw new InvalidDataException("The configuration was not created and saved.");

    var second = load.Invoke(null, [configPath, null, false]);
    if ((Guid)getId.GetValue(second)! != id)
        throw new InvalidDataException("The saved configuration did not round-trip.");

    Console.WriteLine("Verified configuration create/save/load using the packaged shared library.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"iOS configuration verification failed: {exception.GetBaseException().Message}");
    return 1;
}
finally
{
    context.Unload();
    if (Directory.Exists(temporaryDirectory))
        Directory.Delete(temporaryDirectory, recursive: true);
}
