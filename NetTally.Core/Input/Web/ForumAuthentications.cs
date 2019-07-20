using System;
using System.Text;

namespace NetTally.Web
{
    static class ForumAuthentications
    {
        /// <summary>
        /// Gets the cookie associated with the given URI, if available.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="clock">The clock to use for setting the cookie expiration date.</param>
        /// <returns>Returns a cookie if we have one for the given host.  Otherwise, null.</returns>
        /// <exception cref="System.ArgumentNullException">Throws if the URI is null.</exception>
        public static string GetAuthorization(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            string username;
            string password;

            switch (uri.Host)
            {
                case "xf2test.sufficientvelocity.com":
                    username = "xf2demo2019";
                    password = "dBfbyHVvRCsYtLg846r3";
                    break;
                default:
                    return null;
            }

            string encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
            string authorization = "Basic " + encoded;

            return authorization;
        }
    }

}
