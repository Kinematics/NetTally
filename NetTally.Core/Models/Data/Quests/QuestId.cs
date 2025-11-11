using System.ComponentModel;
using System.Text.Json.Serialization;
using NetTally.Models.Converters;

namespace NetTally.Models;

/// <summary>
/// Strongly typed ID based on a GUID for quests.
/// </summary>
/// <param name="Id">A specific GUID to construct a QuestId from.</param>
[JsonConverter(typeof(QuestIdJsonConverter))]
[TypeConverter(typeof(QuestIdTypeConverter))]
public readonly record struct QuestId(Guid Id);
