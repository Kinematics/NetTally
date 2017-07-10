using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.ViewModels;
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
            ["forums.sufficientvelocity.com"] = ForumType.XenForo1,
            ["forums.spacebattles.com"] = ForumType.XenForo1,
            ["forum.questionablequesting.com"] = ForumType.XenForo1,
        };

        /// <summary>
        /// Public function to check for identifiable forums from a provided web page.
        /// </summary>
        /// <param name="uri">The URI being checked.  Cache the host so we don't have to verify again.</param>
        /// <param name="token">Cancellation token for loading page.</param>
        /// <returns>Returns the forum type that was identified, if any.</returns>
        public async static Task<ForumType> IdentifyForumTypeAsync(Uri uri, CancellationToken token)
        {
            if (!forumTypes.TryGetValue(uri.Host, out ForumType forumType))
            {
                HtmlDocument doc = await GetDocumentAsync(uri, token).ConfigureAwait(false);

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
                    else
                    {
                        forumType = ForumType.Unknown;
                    }

                    forumTypes[uri.Host] = forumType;
                }
                else
                {
                    forumType = ForumType.Unknown;
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
        private async static Task<HtmlDocument> GetDocumentAsync(Uri uri, CancellationToken token)
        {
            IPageProvider pageProvider = ViewModelService.MainViewModel.PageProvider;

            try
            {
                HtmlDocument page = await pageProvider.GetPage(uri.AbsoluteUri, uri.Host,
                    CachingMode.UseCache, ShouldCache.Yes, SuppressNotifications.No, token)
                    .ConfigureAwait(false);

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
                Logger.Error("Attempt to query site to determine forum adapter failed.", e);
                return null;
            }
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
    }
}
