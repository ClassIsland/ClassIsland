using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: verify-ios-entry.cs <ClassIsland.iOS.dll> [unlinked-host-assembly ...]");
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

    foreach (var referencePath in args.Skip(1))
    {
        var bundledPath = Path.Combine(Path.GetDirectoryName(args[0])!, Path.GetFileName(referencePath));
        var expected = ReadPublicMembers(referencePath);
        var actual = ReadPublicMembers(bundledPath);
        foreach (var (member, count) in expected)
        {
            if (actual.GetValueOrDefault(member) < count)
                throw new InvalidDataException($"Host API was removed or replaced with a linker stub in {Path.GetFileName(bundledPath)}: {member}.");
        }
        Console.WriteLine($"Verified preserved host API: {Path.GetFileName(bundledPath)} ({expected.Count} checks).");
    }

    Console.WriteLine("Verified iOS entry-point constructor, startup callbacks and IL.");
    return 0;
}
catch (Exception exception) when (exception is IOException or InvalidDataException or BadImageFormatException or InvalidOperationException)
{
    Console.Error.WriteLine($"iOS startup verification failed: {exception.Message}");
    return 1;
}

static Dictionary<string, int> ReadPublicMembers(string path)
{
    using var stream = File.OpenRead(path);
    using var assembly = new PEReader(stream);
    var reader = assembly.GetMetadataReader();
    var members = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var handle in reader.TypeDefinitions)
    {
        if (!IsPublicType(reader, handle))
            continue;
        var type = reader.GetTypeDefinition(handle);
        var typeName = TypeName(reader, handle);
        members[$"type {typeName}"] = 1;
        foreach (var fieldHandle in type.GetFields())
        {
            var field = reader.GetFieldDefinition(fieldHandle);
            if ((field.Attributes & FieldAttributes.FieldAccessMask) is
                FieldAttributes.Public or FieldAttributes.Family or FieldAttributes.FamORAssem)
                members[$"field {typeName}.{reader.GetString(field.Name)}"] = 1;
        }
        foreach (var methodHandle in type.GetMethods())
        {
            var method = reader.GetMethodDefinition(methodHandle);
            if ((method.Attributes & MethodAttributes.MemberAccessMask) is not
                (MethodAttributes.Public or MethodAttributes.Family or MethodAttributes.FamORAssem))
                continue;

            // 按名称、泛型参数数和参数数统计重载，允许注册器添加额外成员。
            var signature = reader.GetBlobReader(method.Signature);
            var header = signature.ReadSignatureHeader();
            var generics = header.IsGeneric ? signature.ReadCompressedInteger() : 0;
            var parameters = signature.ReadCompressedInteger();
            var key = $"method {typeName}.{reader.GetString(method.Name)}`{generics}/{parameters}/{header.IsInstance}";
            members[key] = members.GetValueOrDefault(key) + 1;
            if (method.RelativeVirtualAddress == 0)
                continue;
            var il = assembly.GetMethodBody(method.RelativeVirtualAddress).GetILContent();
            if (il.Length == 0)
                continue;

            // ILLink 可能保留签名，却把 getter 等方法替换成 NotSupportedException。
            var linkedAway = false;
            if (il.Length == 11 && il[0] == 0x72 && il[5] == 0x73 && il[10] == 0x7a)
            {
                var token = il[1] | il[2] << 8 | il[3] << 16 | il[4] << 24;
                linkedAway = reader.GetUserString(MetadataTokens.UserStringHandle(token & 0x00ffffff)) == "Linked away";
            }
            if (!linkedAway)
                members[$"IL {key}"] = members.GetValueOrDefault($"IL {key}") + 1;
        }
    }
    return members;
}

static bool IsPublicType(MetadataReader reader, TypeDefinitionHandle handle)
{
    var type = reader.GetTypeDefinition(handle);
    return (type.Attributes & TypeAttributes.VisibilityMask) switch
    {
        TypeAttributes.Public => true,
        TypeAttributes.NestedPublic or TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem =>
            IsPublicType(reader, type.GetDeclaringType()),
        _ => false
    };
}

static string TypeName(MetadataReader reader, TypeDefinitionHandle handle)
{
    var type = reader.GetTypeDefinition(handle);
    var parent = type.GetDeclaringType();
    return parent.IsNil
        ? $"{reader.GetString(type.Namespace)}.{reader.GetString(type.Name)}"
        : $"{TypeName(reader, parent)}+{reader.GetString(type.Name)}";
}
