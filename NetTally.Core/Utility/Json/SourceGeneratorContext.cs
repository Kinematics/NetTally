using System.Text.Json.Serialization;
using NetTally.Configure;
using NetTally.Product;
using NetTally.Quests;

namespace NetTally.Utility.Json;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(List<GithubRelease>))]
public partial class GithubSourceGeneratorContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    IgnoreReadOnlyProperties = true,
    Converters = [typeof(QuestIdJsonConverter)])]
[JsonSerializable(typeof(ConfigInfo))]
public partial class ConfigInfoSourceGeneratorContext : JsonSerializerContext
{
}