using NetTally.Models.Mapping;

namespace NetTally.Models;

public static class OriginDetailUtility
{
    extension(OriginDetail detail)
    {
        public Uri? GetThread() => detail.Map<Uri?>(
            noDetail => null,
            source => source.Thread);

        public Uri? GetPermalink() => detail.Map<Uri?>(
            noDetail => null,
            source => source.Permalink);

        public PostId GetPostId() => detail.Map(
            noDetail => PostId.None,
            source => source.PostId);

        public PostNumber GetPostNumber() => detail.Map(
            noDetail => PostNumber.None,
            source => source.PostNumber);
    }
}
