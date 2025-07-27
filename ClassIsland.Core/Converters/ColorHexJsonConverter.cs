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

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            byte a = 0;
            byte r = 0;
            byte g = 0;
            byte b = 0;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                var propertyName = reader.GetString();

                reader.Read(); // 读取属性值

                switch (propertyName)
                {
                    case "A":
                        a = reader.GetByte();
                        break;
                    case "R":
                        r = reader.GetByte();
                        break;
                    case "G":
                        g = reader.GetByte();
                        break;
                    case "B":
                        b = reader.GetByte();
                        break;
                    default:
                        reader.Skip(); // 忽略未知字段
                        break;
                }
            }

            return new Color(a, r, g, b);
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