using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using NetTally.Quests;
using NetTally.Types.Enums;

namespace NetTally.Global
{
    public partial class GlobalSettings : ObservableObject
    {
        [ObservableProperty]
        DisplayMode displayMode = DisplayMode.Normal;
        [ObservableProperty]
        bool displayPlansWithNoVotes = false;
        [ObservableProperty]
        bool globalSpoilers = false;
        [ObservableProperty]
        RankVoteCounterMethod rankVoteCounterMethod = RankVoteCounterMethod.Default;

        [ObservableProperty]
        [property: TypeConverter(typeof(BoolExTypeConverter))]
        bool? allowUsersToUpdatePlans = null;
        [ObservableProperty]
        bool trackPostAuthorsUniquely = false;

        [ObservableProperty]
        bool disableWebProxy = false;
        [ObservableProperty]
        [property: JsonIgnore]
        bool debugMode = false;

        public void UpdateFromLegacySettings(GlobalSettings legacySettings)
        {
            DisplayMode = legacySettings.DisplayMode;
            DisplayPlansWithNoVotes= legacySettings.DisplayPlansWithNoVotes;
            GlobalSpoilers= legacySettings.GlobalSpoilers;
            RankVoteCounterMethod = legacySettings.RankVoteCounterMethod;
            AllowUsersToUpdatePlans = legacySettings.AllowUsersToUpdatePlans;
            TrackPostAuthorsUniquely = legacySettings.TrackPostAuthorsUniquely;
            DisableWebProxy = legacySettings.DisableWebProxy;
        }
    }


    public class BoolExTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string val)
            {
                if (int.TryParse(val, out var intVal))
                {
                    bool? result = intVal switch
                    {
                        0 => false,
                        1 => true,
                        _ => null
                    };

                    return result;
                }

                return val switch
                {
                    "true" => true,
                    "false" => false,
                    _ => null
                };
            }

            return base.ConvertFrom(context, culture, value);
        }
    }


    public class BoolExJsonConverter : JsonConverter<bool?>
    {
        public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Null => null,
                JsonTokenType.Number when reader.TryGetInt32(out int n) => n switch
                {
                    0 => false,
                    1 => true,
                    _ => null,
                },
                _ => null
            };
        }

        public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteBooleanValue(value.Value);
            }
        }
    }
}