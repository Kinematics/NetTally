using System;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace NetTally.Quests;

[JsonConverter(typeof(QuestIdJsonConverter))]
public readonly record struct QuestId(Guid Id)
{
    public static readonly QuestId Empty = new(Guid.Empty);
    public static QuestId NewQuestId() => new(Guid.NewGuid());
}


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
