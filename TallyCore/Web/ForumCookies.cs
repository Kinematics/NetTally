using System;
using System.Net;

namespace NetTally
{
    public static class ForumCookies
    {
        /// <summary>
        /// A means of looking up cookies to use for specific forum URLs.
        /// </summary>
        /// <param name="url">URL of the forum thread being loaded.</param>
        /// <returns>Returns a collection of any cookies that need to be set while loading the page.</returns>
        public static CookieCollection GetCookies(string url)
        {
            CookieCollection cookies = new CookieCollection();
            Uri uri = new Uri(url);

            Cookie cookie;

            switch (uri.Host)
            {
                case "forum.questionablequesting.com":
                    // Cookie for vote tally account on QQ, to allow reading the NSFW forums.
                    cookie = new Cookie("xf_user", "2940%2C3f6f04f8921e0b26f3cd6c6399af3a04d3520769", "/", uri.Host);
                    cookie.Expires = DateTime.Now + TimeSpan.FromDays(30);
                    cookies.Add(cookie);
                    break;
            }

            return cookies;
        }

        /// <summary>
        /// Get a collection of all the cookies we know about.  These can all be stored
        /// in the HttpClientManager, and it will use whichever is needed.
        /// </summary>
        /// <returns>Returns the collection of all known cookies.</returns>
        public static CookieCollection GetAllCookies()
        {
            CookieCollection cookies = new CookieCollection();

            Cookie cookie;

            // Cookie for vote tally account on QQ, to allow reading the NSFW forums.
            cookie = new Cookie("xf_user", "2940%2C3f6f04f8921e0b26f3cd6c6399af3a04d3520769", "/", "forum.questionablequesting.com");
            cookie.Expires = DateTime.Now + TimeSpan.FromDays(30);
            cookies.Add(cookie);

            return cookies;
        }
    }
}
