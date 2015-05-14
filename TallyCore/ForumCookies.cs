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
                case "forums.spacebattles.com":
                    cookie = new Cookie("xf_user", "user_hash_placeholder", "/", uri.Host);
                    cookie.Expires = DateTime.Now + TimeSpan.FromDays(30);
                    cookies.Add(cookie);
                    break;
                case "forum.questionablequesting.com":
                    cookie = new Cookie("xf_user", "user_hash_placeholder", "/", uri.Host);
                    cookie.Expires = DateTime.Now + TimeSpan.FromDays(30);
                    cookies.Add(cookie);
                    break;
                default:
                    break;
            }

            return cookies;
        }
    }
}
