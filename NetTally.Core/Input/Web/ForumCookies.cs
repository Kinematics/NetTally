using System;
using System.Net;

namespace NetTally.Web
{
    public static class ForumCookies
    {
        /// <summary>
        /// Gets the cookie associated with the given URI, if available.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="timeProvider">The clock to use for setting the cookie expiration date.</param>
        /// <returns>Returns a cookie if we have one for the given host.  Otherwise, null.</returns>
        /// <exception cref="System.ArgumentNullException">Throws if the URI is null.</exception>
        public static Cookie? GetCookie(Uri uri, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(uri);
            ArgumentNullException.ThrowIfNull(timeProvider);

            Cookie? cookie = null;

            switch (uri.Host)
            {
                case "questionablequesting.com":
                case "forum.questionablequesting.com":
                    // Cookie for vote tally account on QQ, to allow reading the NSFW forums.
                    cookie = new Cookie("xf_user", "2940%2CtxRVjJeJAfio90zmk8tmgxxhnowMExrFaBRUXKlL", "/", uri.Host)
                    {
                        Expires = (timeProvider.GetUtcNow() + TimeSpan.FromDays(30)).DateTime
                    };
                    break;
            }

            return cookie;
        }
    }
}
