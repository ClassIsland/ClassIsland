using System.Text.Json;
using System.Text.Json.Serialization;
namespace ClassIsland.Shared.JsonConverters;

/// <summary>
/// 如果读取到的 Guid 为空白，则转换为 Guid.Empty。
/// </summary>
public class GuidEmptyFallbackConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return string.IsNullOrWhiteSpace(str) ? 
            Guid.Empty : 
            Guid.Parse(str);
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}