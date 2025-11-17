using System.Text.Json.Serialization;

namespace NetTally.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<WPFTheme>))]
public enum WPFTheme
{
    None,
    Light,
    Dark,
    System
}
