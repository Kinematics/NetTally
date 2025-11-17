using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace NetTally.Models.Converters;

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
