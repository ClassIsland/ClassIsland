using System;
using System.Runtime.InteropServices;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

public class OSPlatformTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(OSPlatform);
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer deserializer)
    {
        var scalar = parser.Consume<Scalar>();

        string value = scalar.Value;

        if (value.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            return OSPlatform.Windows;
        if (value.Equals("Linux", StringComparison.OrdinalIgnoreCase))
            return OSPlatform.Linux;
        if (value.Equals("OSX", StringComparison.OrdinalIgnoreCase) || value.Equals("MacOS", StringComparison.OrdinalIgnoreCase))
            return OSPlatform.OSX;

        return OSPlatform.Create(value);
    }

    
    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is not OSPlatform platform)
        {
            emitter.Emit(new Scalar(null));
            return;
        }

        string platformName;

        if (platform.Equals(OSPlatform.Windows)) platformName = "Windows";
        else if (platform.Equals(OSPlatform.Linux)) platformName = "Linux";
        else if (platform.Equals(OSPlatform.OSX)) platformName = "OSX";
        else platformName = platform.ToString();

        emitter.Emit(new Scalar(platformName));
    }
}