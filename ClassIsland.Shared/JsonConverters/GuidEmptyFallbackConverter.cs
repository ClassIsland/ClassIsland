using System.Text.Json;
using System.Text.Json.Serialization;
namespace ClassIsland.Shared.JsonConverters;

/// <summary>
/// 如果读取到的<see cref="Guid"/>为空白，则转换为<see cref="Guid.Empty"/>
/// </summary>
public class GuidEmptyFallbackConverter : JsonConverter<Guid>
{
    /// <summary>
    /// 从 JSON 读取 Guid
    /// </summary>
    /// <returns>返回读取成功的<see cref="Guid"/>对象;如果读取到的<see cref="Guid"/>为空白，则返回<see cref="Guid.Empty"/> </returns>
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return string.IsNullOrWhiteSpace(str) ? 
            Guid.Empty : 
            Guid.Parse(str);
    }

    /// <summary>
    /// 将 Guid 写入 JSON
    /// </summary>
    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}