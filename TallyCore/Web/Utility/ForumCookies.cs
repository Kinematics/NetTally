using System;
using System.Net;
using NetTally.Utility;

namespace NetTally.Web
{
    public static class ForumCookies
    {
        /// <summary>
        /// Gets the cookie associated with the given URI, if available.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="clock">The clock to use for setting the cookie expiration date.</param>
        /// <returns>Returns a cookie if we have one for the given host.  Otherwise, null.</returns>
        /// <exception cref="System.ArgumentNullException">Throws if the URI is null.</exception>
        public static Cookie GetCookie(Uri uri, IClock clock = null)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            clock = clock ?? new DefaultClock();

            Cookie cookie = null;

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
