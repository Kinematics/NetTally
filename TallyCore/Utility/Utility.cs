using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace NetTally.Utility
{

    public static class Text
    {
        // Regex for control and formatting characters that we don't want to allow processing of.
        // EG: \u200B, non-breaking space
        // Do not remove CR/LF characters
        public static Regex UnsafeCharsRegex { get; } = new Regex(@"[\p{C}-[\r\n]]");

        /// <summary>
        /// Filter unsafe characters from the provided string.
        /// </summary>
        /// <param name="input">The string to filter.</param>
        /// <returns>The input string with all unicode control characters (except cr/lf) removed.</returns>
        public static string SafeString(string input)
        {
            return UnsafeCharsRegex.Replace(input, "");
        }

        public static string PlanNameMarker { get; } = "\u25C8";


        #region Web/Post text extraction and cleanup
        // Regex for colors in a span's style
        static readonly Regex spanColorRegex = new Regex(@"\bcolor\s*:\s*(?<color>\w+)", RegexOptions.IgnoreCase);
        // Regex for strike-through in a span's style
        static readonly Regex strikeSpanRegex = new Regex(@"text-decoration:\s*line-through");

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

            StringBuilder sb = new StringBuilder();

            // Recurse into the child nodes of the main post node.
            ExtractChildNodes(sb, node, exclude);

            // Cleanup the results of the extraction.
            return CleanupWebString(sb.ToString());
        }

        /// <summary>
        /// Extract the text of all child nodes of the provided node.
        /// Allows recursive extraction of the post's text.
        /// </summary>
        /// <param name="sb">The stringbuilder where all results are concatenated.</param>
        /// <param name="node">The parent node.</param>
        /// <param name="exclude">A predicate that can be used to exclude specific
        /// sub-nodes from the end result.</param>
        private static void ExtractChildNodes(StringBuilder sb, HtmlNode node, Predicate<HtmlNode> exclude)
        {
            foreach (var childNode in node.ChildNodes)
            {
                ExtractNodeText(sb, childNode, exclude);
            }
        }

        /// <summary>
        /// Extracts the text from the specified node, and converts some elements into BBCode.
        /// </summary>
        /// <param name="sb">The stringbuilder where all results are concatenated.</param>
        /// <param name="node">The parent node.</param>
        /// <param name="exclude">A predicate that can be used to exclude specific
        /// sub-nodes from the end result.</param>
        private static void ExtractNodeText(StringBuilder sb, HtmlNode node, Predicate<HtmlNode> exclude)
        {
            // A raw text node is simply added as-is.
            if (node.Name == "#text")
            {
                sb.Append(node.InnerText);
                return;
            }

            // A <br> element adds a newline.  Sometimes redundant; sometimes necessary.
            if (node.Name == "br")
            {
                sb.AppendLine("");
                return;
            }

            // Check the predicate passed in for custom exclusion of nodes.
            if (exclude(node))
            {
                return;
            }

            // Add BBCode markup in place of HTML format elements, while collecting the text in the post.
            // All remaining elements (except img) recurse into nested nodes to get the actual text.
            switch (node.Name)
            {
                case "i":
                    sb.Append("[i]");
                    ExtractChildNodes(sb, node, exclude);
                    sb.Append("[/i]");
                    break;
                case "b":
                    sb.Append("[b]");
                    ExtractChildNodes(sb, node, exclude);
                    sb.Append("[/b]");
                    break;
                case "u":
                    sb.Append("[u]");
                    ExtractChildNodes(sb, node, exclude);
                    sb.Append("[/u]");
                    break;
                case "span":
                    string spanStyle = node.GetAttributeValue("style", "");

                    // Struck-through text is entirely skipped.
                    if (strikeSpanRegex.Match(spanStyle).Success)
                    {
                        return;
                    }
                    else
                    {
                        // Keep any COLOR styles.
                        Match m = spanColorRegex.Match(spanStyle);
                        if (m.Success)
                        {
                            sb.AppendFormat("[color={0}]", m.Groups["color"].Value);
                            ExtractChildNodes(sb, node, exclude);
                            sb.Append("[/color]");
                        }
                        else
                        {
                            // Take anything else without including span style modifications.
                            ExtractChildNodes(sb, node, exclude);
                        }
                    }
                    break;
                case "a":
                    sb.AppendFormat("[url=\"{0}\"]", node.GetAttributeValue("href", ""));
                    ExtractChildNodes(sb, node, exclude);
                    sb.Append("[/url]");
                    break;
                case "img":
                    string srcUrl = node.GetAttributeValue("data-url", "");
                    if (srcUrl == string.Empty)
                        srcUrl = node.GetAttributeValue("src", "");

                    if (srcUrl != string.Empty)
                    {
                        sb.AppendFormat("[url=\"{0}\"]<Image>[/url]", srcUrl);
                    }
                    break;
                case "div":
                    // Recurse into divs (typically spoilers).
                    ExtractChildNodes(sb, node, exclude);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Clean up problematic bits of text in the extracted HTML string.
        /// </summary>
        /// <param name="text">The text pulled from an HTML page.</param>
        /// <returns>Returns a cleaned version of the post text.</returns>
        public static string CleanupWebString(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            text = text.TrimStart();

            text = HtmlEntity.DeEntitize(text);

            return Utility.Text.SafeString(text);
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

                return classNames.Any(c => nodeClass.Contains(c));
            };

        }
        #endregion

    }
}
