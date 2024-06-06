using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetTally.Quests;

/// <summary>
/// Strongly typed ID based on a GUID for quests.
/// </summary>
/// <param name="Id">A specific GUID to construct a QuestId from.</param>
[JsonConverter(typeof(QuestIdJsonConverter))]
[TypeConverter(typeof(QuestIdTypeConverter))]
public readonly record struct QuestId(Guid Id)
{
    public static readonly QuestId Empty = new(Guid.Empty);
    public static QuestId NewQuestId() => new(Guid.NewGuid());
}

/// <summary>
/// Type converter for QuestId records, to allow loading and saving
/// JSON representation like a GUID.
/// </summary>
public class QuestIdTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        if (sourceType == typeof(Guid) || sourceType == typeof(string))
            return true;

        return base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType)
    {
        if (destinationType == typeof(Guid) || destinationType == typeof(string))
            return true;

        return base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is Guid guid)
            return new QuestId(guid);
        else if (value is string str && Guid.TryParse(str, out var id))
            return new QuestId(id);

        return base.ConvertFrom(context, culture, value);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is QuestId questId)
        {
            if (destinationType == typeof(Guid))
            {
                return questId.Id;
            }
            else if (destinationType == typeof(string))
            {
                return questId.Id.ToString();
            }
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}

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
