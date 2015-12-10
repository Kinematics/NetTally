using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using NetTally.Utility;

namespace NetTally.Adapters
{
    /// <summary>
    /// Class used for extracting usable text out of the raw HTML of a web post.
    /// </summary>
    public static class PostText
    {
        // Regex for colors in a span's style
        static readonly Regex spanColorRegex = new Regex(@"\bcolor\s*:\s*(?<color>#[0-9a-f]+|\w+)", RegexOptions.IgnoreCase);
        // Regex for strike-through in a span's style
        static readonly Regex spanStrikeRegex = new Regex(@"text-decoration:\s*line-through", RegexOptions.IgnoreCase);

        #region Public Functions
        /// <summary>
        /// Clean up problematic bits of text in the extracted HTML string.
        /// </summary>
        /// <param name="text">The text pulled from an HTML page.</param>
        /// <returns>Returns a cleaned version of the post text.</returns>
        public static string CleanupWebString(string text)
        {
            if (text == null)
                return string.Empty;

            text = text.TrimStart();

            text = HtmlEntity.DeEntitize(text);

            return Text.SafeString(text);
        }

        /// <summary>
        /// Extract the text contents of a post, given a starting HTML node.
        /// </summary>
        /// <param name="node">The parent node containing the entirety of a post.</param>
        /// <param name="exclude">A predicate that can be used to exclude specific
        /// sub-nodes from the end result.</param>
        /// <returns>Returns a cleaned version of the text of the post.</returns>
        public static string ExtractPostText(HtmlNode node, Predicate<HtmlNode> exclude)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (exclude == null)
                exclude = (n) => false;

            // Recurse into the child nodes of the main post node.
            string postText = ExtractPostTextString(node, exclude).ToString().Trim();

            // Cleanup the results of the extraction.
            return CleanupWebString(postText);
        }

        /// <summary>
        /// A helper function to create a predicate that excludes a specific class name.
        /// </summary>
        /// <param name="className">The class name to exclude.</param>
        /// <returns>Returns a predicate.</returns>
        public static Predicate<HtmlNode> GetClassExclusionPredicate(string className)
        {
            return (HtmlNode n) =>
            {
                string nodeClass = n.GetAttributeValue("class", "");

                return nodeClass.Contains(className);
            };
        }

        /// <summary>
        /// A helper function to create a predicate that excludes a list of class names.
        /// </summary>
        /// <param name="classNames">The class names to exclude.</param>
        /// <returns>Returns a predicate.</returns>
        public static Predicate<HtmlNode> GetClassesExclusionPredicate(List<string> classNames)
        {
            return (HtmlNode n) =>
            {
                string nodeClass = n.GetAttributeValue("class", "");

                return classNames.Any(nodeClass.Contains);
            };

        }

        #endregion

        #region Private Support Functions
        /// <summary>
        /// Extracts the text (recursively) from the specified node, and converts some elements into BBCode.
        /// </summary>
        /// <param name="node">The parent node.</param>
        /// <param name="exclude">A predicate that can be used to exclude specific
        /// sub-nodes from the end result.</param>
        /// <param name="sb">The stringbuilder where all results are concatenated.  Will create if not provided.</param>
        /// <returns>Returns a StringBuilder containing the results of converting the HTML to text (with possible BBCode).</returns>
        private static StringBuilder ExtractPostTextString(HtmlNode node, Predicate<HtmlNode> exclude, StringBuilder sb = null)
        {
            if (sb == null)
                sb = new StringBuilder();

            foreach (var child in node.ChildNodes)
            {
                if (exclude(child))
                {
                    continue;
                }

                switch (child.Name)
                {
                    case "#text":
                        sb.Append(child.InnerText);
                        break;
                    case "br":
                        sb.Append("\r\n");
                        break;
                    case "i":
                        sb.Append("[i]");
                        ExtractPostTextString(child, exclude, sb);
                        sb.Append("[/i]");
                        break;
                    case "b":
                        sb.Append("[b]");
                        ExtractPostTextString(child, exclude, sb);
                        sb.Append("[/b]");
                        break;
                    case "u":
                        sb.Append("[u]");
                        ExtractPostTextString(child, exclude, sb);
                        sb.Append("[/u]");
                        break;
                    case "span":
                        string spanStyle = child.GetAttributeValue("style", "");

                        // Struck-through text is entirely skipped.
                        if (spanStrikeRegex.Match(spanStyle).Success)
                        {
                            continue;
                        }
                        else
                        {
                            // Keep any COLOR styles.
                            Match m = spanColorRegex.Match(spanStyle);
                            if (m.Success)
                            {
                                sb.Append($"[color={m.Groups["color"].Value}]");
                                ExtractPostTextString(child, exclude, sb);
                                sb.Append("[/color]");
                            }
                            else
                            {
                                // Take anything else without including span style modifications.
                                ExtractPostTextString(child, exclude, sb);
                            }
                        }
                        break;
                    case "a":
                        sb.Append($"[url=\"{child.GetAttributeValue("href", "")}\"]");
                        ExtractPostTextString(child, exclude, sb);
                        sb.Append("[/url]");
                        break;
                    case "img":
                        string srcUrl = child.GetAttributeValue("data-url", "");
                        if (srcUrl == string.Empty)
                            srcUrl = child.GetAttributeValue("src", "");

                        // MCE sprite smilies do not use actual images, so don't include them.
                        if (child.GetAttributeValue("class", "").Contains("mceSmilieSprite"))
                            break;

                        if (srcUrl != string.Empty)
                        {
                            sb.Append($"[url=\"{srcUrl}\"]<Image>[/url]");
                        }
                        break;
                    case "div":
                        // Recurse into divs (typically spoilers).
                        ExtractPostTextString(child, exclude, sb);
                        break;
                }
            }

            return sb;
        }
        #endregion
    }
}
