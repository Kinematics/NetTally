using System;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace NetTally.Quests;
public readonly record struct QuestId(Guid Id)
{
    public static readonly QuestId Empty = new(Guid.Empty);
    public static QuestId NewQuestId() => new(Guid.NewGuid());
}


public class QuestIdJsonConverter : JsonConverter<QuestId>
{
    public override QuestId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
            return QuestId.Empty;

        var value = JsonSerializer.Deserialize<Guid>(ref reader, options);
        return new QuestId(value);
    }

    public override void Write(Utf8JsonWriter writer, QuestId value, JsonSerializerOptions options)
    {
        if (value == QuestId.Empty)
            writer.WriteNullValue();
        else
            JsonSerializer.Serialize(writer, value.Id, options);
    }
}

