using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetTally.Models.Converters;

/// <summary>
/// JSON converter for QuestId records, to allow loading and saving
/// JSON representation like a GUID.
/// </summary>
public class QuestIdJsonConverter : JsonConverter<QuestId>
{
    public override QuestId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Guid value = reader.GetGuid();
        return new QuestId(value);
    }

    public override void Write(Utf8JsonWriter writer, QuestId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Id);
    }
}
