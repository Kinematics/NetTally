using NetTally.Models.Mapping;
using NetTally.Utility.Strings;

namespace NetTally.Models;

public static class SourceDisplay
{
    extension(Source source)
    {
        public string GetBBCodeLink(string displayName) => source.Map(
                noSource => string.Empty,
                locationSource => urlTemplate.FormatWith(locationSource.Permalink, displayName)
                );
    }

    static readonly string urlTemplate = "[url=\"{0}\"]{1}[/url]";
}
