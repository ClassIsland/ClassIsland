using System.Text.Json;
using System.Text.Json.Serialization;
using ClassIsland.Shared.Enums;
namespace ClassIsland.Shared.JsonConverters;

/// <summary>
/// 用于避免保存和读取 <see cref="ActionSetStatus"/> 正在运行状态的 <see cref="JsonConverter"/>。
/// </summary>
public class ActionSetStatusJsonConverter : JsonConverter<ActionSetStatus>
{
    static ActionSetStatus ConvertStatus(ActionSetStatus status) => status switch
    {
        ActionSetStatus.Invoking => ActionSetStatus.Normal,
        ActionSetStatus.Reverting => ActionSetStatus.IsOn,
        _ => status
    };

    /// <inheritdoc />
    public override ActionSetStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return ConvertStatus((ActionSetStatus)reader.GetInt32());
        }

        if (reader.TokenType == JsonTokenType.String &&
            Enum.TryParse(reader.GetString(), out ActionSetStatus status))
        {
            return ConvertStatus(status);
        }

        throw new JsonException($"Invalid value for ActionSetStatus: ({reader.TokenType}){reader.GetString()}");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ActionSetStatus value, JsonSerializerOptions options)
    {
        var convertedValue = ConvertStatus(value);
        writer.WriteNumberValue((int)convertedValue);
        // writer.WriteStringValue(convertedValue.ToString());
    }
}