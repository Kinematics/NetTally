using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally.Forums
{
    /// <summary>
    /// Class used to scan a Uri and HTML document to determine which forum type was used to generate it.
    /// </summary>
    public static class ForumIdentifier
    {
        static Dictionary<string, ForumType> cacheHosts = new Dictionary<string, ForumType>();

        /// <summary>
        /// Public function to check for identifiable forums from a provided web page.
        /// </summary>
        /// <param name="uri">The URI being checked.  Cache the host so we don't have to verify again.</param>
        /// <param name="token">Cancellation token for loading page.</param>
        /// <returns>Returns the forum type that was identified, if any.</returns>
        public async static Task<ForumType> IdentifyForumTypeAsync(Uri uri, CancellationToken token)
        {
            if (uri.IsAbsoluteUri)
            {
                if (cacheHosts.TryGetValue(uri.Host, out ForumType forumType))
                {
                    if (forumType != ForumType.Unknown)
                        return forumType;
                }
            }

            ForumType forum = ForumType.Unknown;

            if (!CheckForKnownHost(uri, out forum))
            {
                HtmlDocument doc = await GetDocumentAsync(uri, token);

                if (doc != null)
                {
                    if (IdentifyXenForo(doc))
                    {
                        forum = ForumType.XenForo;
                    }
                    else if (IdentifyVBulletin3(doc))
                    {
                        forum = ForumType.vBulletin3;
                    }
                    else if (IdentifyVBulletin4(doc))
                    {
                        forum = ForumType.vBulletin4;
                    }
                    else if (IdentifyVBulletin5(doc))
                    {
                        forum = ForumType.vBulletin5;
                    }
                }
            }

            if (forum != ForumType.Unknown)
            {
                cacheHosts[uri.Host] = forum;
            }

            return forum;
        }

        /// <summary>
        /// Check for known forums for simplicity.
        /// </summary>
        /// <param name="uri">The Uri being checked.</param>
        /// <param name="forum">The forum type the URI belongs to, if known.</param>
        /// <returns>Returns true if the URI is to a known host.</returns>
        private static bool CheckForKnownHost(Uri uri, out ForumType forum)
        {
            switch (uri.Host)
            {
                // Known XenForo sites
                case "forums.sufficientvelocity.com":
                case "forums.spacebattles.com":
                case "forum.questionablequesting.com":
                    forum = ForumType.XenForo;
                    return true;
                default:
                    forum = ForumType.Unknown;
                    return false;
            }
        }

        /// <summary>
        /// Get the HTML document for the specified URI.
        /// </summary>
        /// <param name="uri">The URI to load.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Returns the requested page, if found. Otherwise, null.</returns>
        private async static Task<HtmlDocument> GetDocumentAsync(Uri uri, CancellationToken token)
        {
            IPageProvider pageProvider = ViewModels.ViewModelService.MainViewModel.PageProvider;

            try
            {
                HtmlDocument page = await pageProvider.GetPage(uri.AbsoluteUri, uri.Host,
                    CachingMode.UseCache, ShouldCache.Yes, SuppressNotifications.No, token);

                if (token.IsCancellationRequested)
                    return null;

                return page;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception e)
            {
                ErrorLog.Log(e);
                return null;
            }
        }

        /// <summary>
        /// Determine if a web page is from a XenForo forum.
        /// </summary>
        /// <param name="doc">The HTML page to check.</param>
        /// <returns>Returns true if the HTML is XenForo</returns>
        private static bool IdentifyXenForo(HtmlDocument doc)
        {
            if (doc == null)
                return false;

            return (doc.DocumentNode.Element("html").Id == "XenForo");
        }

        /// <summary>
        /// Determine if a web page is from a vBulletin 3 forum.
        /// </summary>
        /// <param name="doc">The HTML page to check.</param>
        /// <returns>Returns true if the HTML is vBulletin 3</returns>
        private static bool IdentifyVBulletin3(HtmlDocument doc)
        {
            if (doc == null)
                return false;

            var html = doc.DocumentNode.Element("html");
            if (!string.IsNullOrEmpty(html.Id))
                return false;

            var head = html.Element("head");
            if (head != null)
            {
                var generator = head.Elements("meta").FirstOrDefault(a => a.GetAttributeValue("name", "") == "generator");
                if (generator != null)
                {
                    if (generator.GetAttributeValue("content", "").StartsWith("vBulletin", StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determine if a web page is from a vBulletin 4 forum.
        /// </summary>
        /// <param name="doc">The HTML page to check.</param>
        /// <returns>Returns true if the HTML is vBulletin 4</returns>
        private static bool IdentifyVBulletin4(HtmlDocument doc)
        {
            if (doc == null)
                return false;

            return doc.DocumentNode.Element("html")?.Id == "vbulletin_html";
        }

        /// <summary>
        /// Determine if a web page is from a vBulletin 5 forum.
        /// </summary>
        /// <param name="doc">The HTML page to check.</param>
        /// <returns>Returns true if the HTML is vBulletin 5</returns>
        private static bool IdentifyVBulletin5(HtmlDocument doc)
        {
            if (doc == null)
                return false;

            return doc.DocumentNode.Element("html").Element("body")?.Id == "vb-page-body";
        }
    }
}
