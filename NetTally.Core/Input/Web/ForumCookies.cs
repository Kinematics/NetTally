using System;
using System.Net;
using NetTally.SystemInfo;

namespace NetTally.Web
{
    public static class ForumCookies
    {
        /// <summary>
        /// Shortcut that provides the default clock parameter.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        public static Cookie? GetCookie(Uri uri) => GetCookie(uri, new SystemClock());

        /// <summary>
        /// Gets the cookie associated with the given URI, if available.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="clock">The clock to use for setting the cookie expiration date.</param>
        /// <returns>Returns a cookie if we have one for the given host.  Otherwise, null.</returns>
        /// <exception cref="System.ArgumentNullException">Throws if the URI is null.</exception>
        public static Cookie? GetCookie(Uri uri, IClock clock)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            if (clock == null)
                throw new ArgumentNullException(nameof(clock));

            Cookie? cookie = null;

            switch (uri.Host)
            {
                case "forum.questionablequesting.com":
                    // Cookie for vote tally account on QQ, to allow reading the NSFW forums.
                    cookie = new Cookie("xf_user", "2940%2C3f6f04f8921e0b26f3cd6c6399af3a04d3520769", "/", uri.Host);
                    cookie.Expires = clock.Now + TimeSpan.FromDays(30);
                    break;
            }

            return cookie;
        }
    }
}
