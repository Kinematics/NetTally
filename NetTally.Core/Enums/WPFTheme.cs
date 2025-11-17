using System.Text.Json.Serialization;

namespace NetTally.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<WPFTheme>))]
public enum WPFTheme
{
    System,
    Light,
    Dark,
    //None,
}
