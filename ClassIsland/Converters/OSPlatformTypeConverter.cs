using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using ClassIsland.Core.Enums;

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
        if (value.Equals("OSX", StringComparison.OrdinalIgnoreCase) || value.Equals("macOS", StringComparison.OrdinalIgnoreCase))
            return OSPlatform.macOS;
        if (value.Equals("Android", StringComparison.OrdinalIgnoreCase))
            return OSPlatform.Android;
        if (value.Equals("iOS", StringComparison.OrdinalIgnoreCase))
            return OSPlatform.iOS;

        return OSPlatform.Unknown;
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
        else if (platform.Equals(OSPlatform.macOS)) platformName = "macOS";
        else if (platform.Equals(OSPlatform.Android)) platformName = "Android";
        else if (platform.Equals(OSPlatform.iOS)) platformName = "iOS";
        else platformName = "Unknown";

        emitter.Emit(new Scalar(platformName));
    }
}