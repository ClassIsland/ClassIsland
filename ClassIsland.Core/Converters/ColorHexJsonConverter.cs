using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using Avalonia.Controls.Converters;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace ClassIsland.Core.Converters;

/// <summary>
/// 适用于 <see cref="Color"/> 的值转换器。
/// </summary>
public class ColorHexJsonConverter : JsonConverter<Color>
{
    /// <inheritdoc />
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return ColorToHexConverter.ParseHexString(reader.GetString() ?? "", AlphaComponentPosition.Trailing) ?? default;
        }
        
        reader.Skip();
        return default;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(ColorToHexConverter.ToHexString(value, AlphaComponentPosition.Trailing, includeSymbol:true));
    }
}