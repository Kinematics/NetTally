using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Web;

namespace NetTally.Forums
{
    /// <summary>
    /// Class used to scan a Uri and HTML document to determine which forum type was used to generate it.
    /// </summary>
    static class ForumIdentifier
    {
        static readonly Dictionary<string, ForumType> forumTypes = new Dictionary<string, ForumType>
        {
        };

        /// <summary>
        /// Public function to check for identifiable forums from a provided web page.
        /// </summary>
        /// <param name="uri">The URI being checked.  Cache the host so we don't have to verify again.</param>
        /// <param name="token">Cancellation token for loading page.</param>
        /// <returns>Returns the forum type that was identified, if any.</returns>
        public static async Task<ForumType> IdentifyForumTypeAsync(Uri? uri, IPageProvider pageProvider, CancellationToken token)
        {
            if (uri == null)
                return ForumType.Unknown;

            if (!forumTypes.TryGetValue(uri.Host, out ForumType forumType))
            {
                var doc = await GetDocumentAsync(uri, pageProvider, token).ConfigureAwait(false);

                if (doc == null)
                {
                    ArgumentException e = new ArgumentException($"Unable to load forum URL:  {uri.AbsoluteUri}");
                    e.Data["Notify"] = true;
                    throw e;
                }

                forumType = IdentifyForumTypeFromHtmlDocument(doc);

                forumTypes[uri.Host] = forumType;
            }

            return forumType;
        }

        /// <summary>
        /// Function to check a provided HTML document and use it to determine a forum type.
        /// </summary>
        /// <param name="doc">An HTML document.</param>
        /// <returns>Returns a forum type, if it can be determined based on the provided document.</returns>
        public static ForumType IdentifyForumTypeFromHtmlDocument(HtmlDocument? doc)
        {
            ForumType forumType = ForumType.Unknown;

            if (doc != null)
            {
                if (IdentifyXenForo1(doc))
                {
                    forumType = ForumType.XenForo1;
                }
                else if (IdentifyXenForo2(doc))
                {
                    forumType = ForumType.XenForo2;
                }
                else if (IdentifyVBulletin3(doc))
                {
                    forumType = ForumType.vBulletin3;
                }
                else if (IdentifyVBulletin4(doc))
                {
                    forumType = ForumType.vBulletin4;
                }
                else if (IdentifyVBulletin5(doc))
                {
                    forumType = ForumType.vBulletin5;
                }
                else if (IdentifyPhpBB(doc))
                {
                    forumType = ForumType.phpBB;
                }
                else if (IdentifyNodeBB(doc))
                {
                    forumType = ForumType.NodeBB;
                }
            }

            return forumType;
        }

        /// <summary>
        /// Get the HTML document for the specified URI.
        /// </summary>
        /// <param name="uri">The URI to load.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Returns the requested page, if found. Otherwise, null.</returns>
        private async static Task<HtmlDocument?> GetDocumentAsync(Uri uri, IPageProvider pageProvider, CancellationToken token)
        {
            HtmlDocument? page = null;

            try
            {
                page = await pageProvider.GetHtmlDocumentAsync(uri.AbsoluteUri, uri.Host,
                    CachingMode.UseCache, ShouldCache.Yes, SuppressNotifications.Yes, token)
                    .ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    page = null;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Logger.Error("Attempt to query site to determine forum adapter failed.", e);
            }

            return page;
        }

        /// <summary>
        /// Determine if a web page is from a XenForo forum.
        /// </summary>
        /// <param name="doc">The HTML page to check.</param>
        /// <returns>Returns true if the HTML is XenForo</returns>
        private static bool IdentifyXenForo1(HtmlDocument doc)
        {
            if (doc == null)
                return false;

            return (doc.DocumentNode.Element("html").Id == "XenForo");
        }

        /// <summary>
        /// Determine if a web page is from a XenForo2 forum.
        /// </summary>
        /// <param name="doc">The HTML page to check.</param>
        /// <returns>Returns true if the HTML is XenForo</returns>
        private static bool IdentifyXenForo2(HtmlDocument doc)
        {
            if (doc == null)
                return false;

            return (doc.DocumentNode.Element("html").Id == "XF");
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

        /// <summary>
        /// Determine if a web page is from a phpBB forum.
        /// </summary>
        /// <param name="doc">The HTML page to check.</param>
        /// <returns>Returns true if the HTML is phpBB</returns>
        private static bool IdentifyPhpBB(HtmlDocument doc)
        {
            if (doc == null)
                return false;

            return doc.DocumentNode.Element("html").Element("body")?.Id == "phpbb";
        }

        /// <summary>
        /// Determine if a web page is from a NodeBB forum.
        /// </summary>
        /// <param name="doc">The HTML page to check.</param>
        /// <returns>Returns true if the HTML is NodeBB</returns>
        private static bool IdentifyNodeBB(HtmlDocument doc)
        {
            if (doc == null)
                return false;

            // There is currently no known means of identifying NodeBB forums.
            return false;
        }
    }
}
