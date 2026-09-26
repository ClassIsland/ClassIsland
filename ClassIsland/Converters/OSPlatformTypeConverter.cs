using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using ClassIsland.Core.Enums;
using OSPlatform = ClassIsland.Core.Enums.OSPlatform;

public class OSPlatformTypeConverter_Yaml : IYamlTypeConverter
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
public class OSPlatformConverter_Json : JsonConverter<System.Runtime.InteropServices.OSPlatform>
{
    public override System.Runtime.InteropServices.OSPlatform Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string platformStr = reader.GetString();
        return platformStr switch
        {
            "Windows" => System.Runtime.InteropServices.OSPlatform.Windows,
            "Linux" => System.Runtime.InteropServices.OSPlatform.Linux,
            "OSX" => System.Runtime.InteropServices.OSPlatform.OSX,
            "FreeBSD" => System.Runtime.InteropServices.OSPlatform.FreeBSD,
            _ => System.Runtime.InteropServices.OSPlatform.Create(platformStr)
        };
    }

    public override void Write(Utf8JsonWriter writer, System.Runtime.InteropServices.OSPlatform value, JsonSerializerOptions options)
    {
        string valuestring = "";
        if (value == System.Runtime.InteropServices.OSPlatform.Windows) valuestring = "Windows";
        else if (value == System.Runtime.InteropServices.OSPlatform.Linux) valuestring = "Linux";
        else if (value == System.Runtime.InteropServices.OSPlatform.OSX) valuestring = "OSX";
        else if (value == System.Runtime.InteropServices.OSPlatform.FreeBSD) valuestring = "FreeBSD";
        else valuestring = value.ToString();
        writer.WriteStringValue(valuestring);
    }
}