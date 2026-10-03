using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: verify-ios-entry.cs <ClassIsland.iOS.dll>");
    return 64;
}

try
{
    // 只读取最终包的元数据和 IL，不在 CI 主机上加载或执行 iOS 程序集。
    using var stream = File.OpenRead(args[0]);
    using var assembly = new PEReader(stream);
    var reader = assembly.GetMetadataReader();
    var requiredMethods = new Dictionary<string, string[]>
    {
        ["ClassIsland.iOS.AppDelegate"] = [".ctor", "CreateAppBuilder", "CustomizeAppBuilder"],
        ["ClassIsland.iOS.MainClass"] = ["Main", "RunApplication"]
    };

    foreach (var (typeName, methodNames) in requiredMethods)
    {
        var typeHandle = reader.TypeDefinitions.FirstOrDefault(handle =>
        {
            var type = reader.GetTypeDefinition(handle);
            return $"{reader.GetString(type.Namespace)}.{reader.GetString(type.Name)}" == typeName;
        });
        if (typeHandle.IsNil)
            throw new InvalidDataException($"Required iOS startup type was removed: {typeName}.");

        var methods = reader.GetTypeDefinition(typeHandle).GetMethods()
            .Select(reader.GetMethodDefinition).ToArray();
        foreach (var methodName in methodNames)
        {
            var matches = methods.Where(method => reader.GetString(method.Name) == methodName);
            if (methodName == ".ctor")
            {
                matches = matches.Where(method =>
                    (method.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public &&
                    method.GetParameters().All(handle => reader.GetParameter(handle).SequenceNumber == 0));
            }

            if (!matches.Any(method => method.RelativeVirtualAddress != 0 &&
                                      assembly.GetMethodBody(method.RelativeVirtualAddress).GetILContent().Length > 0))
                throw new InvalidDataException($"Required iOS startup method or IL was removed: {typeName}.{methodName}.");
        }
    }

    Console.WriteLine("Verified iOS entry-point constructor, startup callbacks and IL.");
    return 0;
}
catch (Exception exception) when (exception is IOException or InvalidDataException or BadImageFormatException or InvalidOperationException)
{
    Console.Error.WriteLine($"iOS startup verification failed: {exception.Message}");
    return 1;
}
