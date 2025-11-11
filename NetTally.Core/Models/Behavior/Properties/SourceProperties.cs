using NetTally.Models.Mapping;

namespace NetTally.Models;

public static class SourceProperties
{
    extension(Source source)
    {
        public Uri? Thread => source.Map<Uri?>(
            noSource => null,
            sourceLocation => sourceLocation.Thread);

        public Uri? Permalink => source.Map<Uri?>(
            noSource => null,
            sourceLocation => sourceLocation.Permalink);

        public PostId PostId => source.Map(
            noSource => Models.PostId.None,
            sourceLocation => sourceLocation.PostId);

        public PostNumber PostNumber => source.Map(
            noSource => Models.PostNumber.None,
            sourceLocation => sourceLocation.PostNumber);
    }
}
