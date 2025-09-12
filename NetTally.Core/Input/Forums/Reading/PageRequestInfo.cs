using NetTally.Enums;

namespace NetTally.Input.Forums.Reading;
public record PageRequestInfo(string Url, int PageNumber, CachingMode CacheMode);

public static class PageRequestInfoCreation
{
    extension(PageRequestInfo)
    {
        public static PageRequestInfo Create(
            string url,
            int pageNumber,
            CachingMode cachingMode)
        {
            return new(url, pageNumber, cachingMode);
        }
    }
}
