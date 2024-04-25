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
                    cookie = new Cookie("xf_user", "2940%2C3f6f04f8921e0b26f3cd6c6399af3a04d3520769", "/", uri.Host)
                    {
                        Expires = (timeProvider.GetUtcNow() + TimeSpan.FromDays(30)).DateTime
                    };
                    break;
                case "xf2.questionablequesting.com":
                    cookie = new Cookie("xf_user", "2940%2CZKfOlFI_iQ5kQXU3FVeg4GzE2Y-wS0-V7y3fsvI6", "/", uri.Host)
                    {
                        Expires = (timeProvider.GetUtcNow() + TimeSpan.FromDays(30)).DateTime
                    };
                    break;
            }

            return cookie;
        }
    }
}
