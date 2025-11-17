using System.Text.Json.Serialization;

namespace NetTally.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<AvaloniaTheme>))]
public enum AvaloniaTheme
{
    Default,
    Light,
    Dark,
}
