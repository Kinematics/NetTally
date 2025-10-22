using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using NetTally.Utility;

using static NetTally.Configure.Strings;

namespace NetTally.Input.Forums.ForumAdapters;

/// <summary>
/// Class used for extracting usable text out of the raw HTML of a web post.
/// </summary>
static partial class ForumPostTextConverter
{
    #region Regex
    // Regex for colors in a span's style
    [GeneratedRegex(@"\bcolor\s*:\s*(?<color>#[0-9a-f]+|\w+)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex SpanColorRegex { get; }

    // Regex for strike-through in a span's style
    [GeneratedRegex(@"text-decoration:\s*line-through", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex SpanStrikeRegex { get; }

    // Regex for quick spoilers in a span's class
    [GeneratedRegex(@"bbc-spoiler", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex SpanSpoilerRegex { get; }
    #endregion Regex

    #region Public Functions
    /// <summary>
    /// Clean up problematic bits of text in the extracted HTML string.
    /// </summary>
    /// <param name="text">The text pulled from an HTML page.</param>
    /// <returns>Returns a cleaned version of the post text.</returns>
    public static string CleanupWebString(string? text)
    {
        if (text == null)
            return string.Empty;

        text = text.TrimStart(' ', '\t');

        text = HtmlEntity.DeEntitize(text);

        return text?.RemoveUnsafeCharacters() ?? "";
    }

    /// <summary>
    /// Extract the text contents of a post, given a starting HTML node.
    /// </summary>
    /// <param name="node">The parent node containing the entirety of a post.  Cannot be null.</param>
    /// <param name="exclude">A predicate that can be used to exclude specific
    /// sub-nodes from the end result.  A default is used if none is provided.</param>
    /// <returns>Returns a cleaned version of the text of the post.</returns>
    /// <exception cref="ArgumentNullException">If node is null.</exception>
    public static string ExtractPostText(HtmlNode? node, Func<HtmlNode, bool>? exclude, Uri host)
    {
        ArgumentNullException.ThrowIfNull(node);

        // If no exclusion is provided, no nodes are removed.
        exclude ??= (n) => false;

        // Recurse into the child nodes of the main post node.
        string postText = ExtractPostTextString(node, exclude, host);

        // Cleanup the results of the extraction.
        return CleanupWebString(postText);
    }

    /// <summary>
    /// A helper function to create a predicate that excludes a specific class name.
    /// </summary>
    /// <param name="className">The class name to exclude.</param>
    /// <returns>Returns a predicate.</returns>
    public static Func<HtmlNode, bool> GetClassExclusionPredicate(string className)
    {
        return (HtmlNode n) =>
        {
            var nodeClasses = n.GetAttributeValue("class", "").Split(' ');
            return nodeClasses.Contains(className, StringComparer.OrdinalIgnoreCase);
        };
    }

    /// <summary>
    /// A helper function to create a predicate that excludes a list of class names.
    /// </summary>
    /// <param name="classNames">The class names to exclude.</param>
    /// <returns>Returns a predicate.</returns>
    public static Func<HtmlNode, bool> GetClassesExclusionPredicate(List<string> classNames)
    {
        return (HtmlNode n) =>
        {
            var nodeClasses = n.GetAttributeValue("class", "").Split(' ');
            return classNames.Any(p => nodeClasses.Contains(p, StringComparer.OrdinalIgnoreCase));
        };
    }

    #endregion

    #region Private Support Functions
    static readonly char[] newlineChars = ['\r', '\n'];
    const string normalNewline = "\r\n";

    static readonly string openItalics = $"{OpenBBCode}i{CloseBBCode}";
    static readonly string closeItalics = $"{OpenBBCode}/i{CloseBBCode}";
    static readonly string openBold = $"{OpenBBCode}b{CloseBBCode}";
    static readonly string closeBold = $"{OpenBBCode}/b{CloseBBCode}";
    static readonly string openUnderline = $"{OpenBBCode}u{CloseBBCode}";
    static readonly string closeUnderline = $"{OpenBBCode}/u{CloseBBCode}";
    static readonly string openQuickSpoilers = $"{OpenBBCode}qs{CloseBBCode}";
    static readonly string closeQuickSpoilers = $"{OpenBBCode}/qs{CloseBBCode}";

    static readonly string urlTemplate = $"{OpenBBCode}url=\"{{0}}\"{CloseBBCode}";
    static readonly string colorTemplate = $"{OpenBBCode}color=\"{{0}}\"{CloseBBCode}";
    static readonly string imageTemplate = $"{OpenBBCode}url=\"{{0}}\"{CloseBBCode}<Image>{OpenBBCode}/url{CloseBBCode}";

    static readonly string closeUrl = $"{OpenBBCode}/url{CloseBBCode}";
    static readonly string closeColor = $"{OpenBBCode}/color{CloseBBCode}";

    extension(string template)
    {
        string FormatWith(string prm)
        {
            string result = FormattableStringFactory.Create(template, prm).ToString();
            return result;
        }
    }

    /// <summary>
    /// Extracts post text as a string from the provided HTML node.
    /// Creates a new string builder to call the full version of this function.
    /// </summary>
    /// <param name="node">The starting HTML node.</param>
    /// <param name="exclude">A predicate to exclude processing of further nodes.</param>
    /// <returns>Returns the text contents of the post.</returns>
    private static string ExtractPostTextString(HtmlNode node, Func<HtmlNode, bool> exclude, Uri host) =>
        ExtractPostTextString(node, exclude, new StringBuilder(), host);

    /// <summary>
    /// Extracts the text (recursively) from the specified node, and converts some elements into BBCode.
    /// </summary>
    /// <param name="node">The parent node.</param>
    /// <param name="exclude">A predicate that can be used to exclude specific
    /// sub-nodes from the end result.</param>
    /// <param name="sb">The stringbuilder where all results are concatenated.</param>
    /// <returns>Returns a StringBuilder containing the results of converting the HTML to text (with possible BBCode).</returns>
    private static string ExtractPostTextString(HtmlNode node,
                                                Func<HtmlNode, bool> exclude,
                                                StringBuilder sb,
                                                Uri host)
    {
        System.Diagnostics.Debug.Assert(node != null);
        System.Diagnostics.Debug.Assert(exclude != null);
        System.Diagnostics.Debug.Assert(sb != null);

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
                    sb.Append(normalNewline);
                    break;
                case "i":
                    sb.Append(openItalics);
                    ExtractPostTextString(child, exclude, sb, host);
                    sb.Append(closeItalics);
                    break;
                case "b":
                    sb.Append(openBold);
                    ExtractPostTextString(child, exclude, sb, host);
                    sb.Append(closeBold);
                    break;
                case "u":
                    sb.Append(openUnderline);
                    ExtractPostTextString(child, exclude, sb, host);
                    sb.Append(closeUnderline);
                    break;
                case "span":
                    string spanStyle = child.GetAttributeValue("style", "");
                    string spanClass = child.GetAttributeValue("class", "");

                    // Struck-through text is entirely skipped.
                    if (SpanStrikeRegex.Match(spanStyle).Success)
                    {
                        sb.Append(OpenStrike);
                        ExtractPostTextString(child, exclude, sb, host);
                        sb.Append(CloseStrike);
                    }
                    else if (SpanSpoilerRegex.Match(spanClass).Success)
                    {
                        // Keep quick spoilers.
                        sb.Append(openQuickSpoilers);
                        ExtractPostTextString(child, exclude, sb, host);
                        sb.Append(closeQuickSpoilers);
                    }
                    else
                    {
                        // Keep any COLOR styles.
                        Match m = SpanColorRegex.Match(spanStyle);
                        if (m.Success)
                        {
                            sb.Append(colorTemplate.FormatWith(m.Groups["color"].Value));
                            ExtractPostTextString(child, exclude, sb, host);
                            sb.Append(closeColor);
                        }
                        else
                        {
                            // Take anything else without including span style modifications.
                            ExtractPostTextString(child, exclude, sb, host);
                        }
                    }
                    break;
                case "a":
                    sb.Append(urlTemplate.FormatWith(Uri.UnescapeDataString(child.GetAttributeValue("href", ""))));
                    ExtractPostTextString(child, exclude, sb, host);
                    sb.Append(closeUrl);
                    break;
                case "img":
                    string srcUrl = child.GetAttributeValue("data-url", "");
                    if (string.IsNullOrEmpty(srcUrl))
                        srcUrl = child.GetAttributeValue("src", "");

                    // MCE sprite smilies do not use actual images, so don't include them.
                    if (child.GetAttributeValue("class", "").Contains("mceSmilieSprite"))
                        break;

                    string imgHref = srcUrl;

                    if (!string.IsNullOrEmpty(srcUrl))
                    {
                        try
                        {
                            // If the source URL is relative, prepend the forum's host.
                            // This will not modify absolute URLs.
                            Uri absoluteSrc = new(host, Uri.UnescapeDataString(srcUrl));
                            imgHref = absoluteSrc.ToString();
                        }
                        catch (UriFormatException)
                        {
                        }
                    }

                    sb.Append(imageTemplate.FormatWith(imgHref));
                    break;
                case "div":
                    // Recurse into divs (typically spoilers).
                    ExtractPostTextString(child, exclude, sb, host);
                    sb.Append(normalNewline);
                    break;
            }
        }

        return StripDuplicateNewlines(sb.ToString());
    }

    private static string StripDuplicateNewlines(ReadOnlySpan<char> input)
    {
        StringBuilder sb = new();

        bool newlineState = false;
        bool strikeState = false;
        int bufferStart = 0;

        for (int c = 0; c < input.Length; c++)
        {
            char ch = input[c];

            if (newlineChars.Contains(ch))
            {
                if (!newlineState)
                {
                    sb.Append(input[bufferStart..c]);

                    if (strikeState)
                        sb.Append(StrikeNewLine);
                    else
                        sb.Append(normalNewline);

                    newlineState = true;
                }
            }
            else if (newlineState)
            {
                bufferStart = c;
                newlineState = false;
            }

            if (ch == OpenStrike)
            {
                strikeState = true;
            }
            else if (ch == CloseStrike)
            {
                strikeState = false;
            }
        }

        if (!newlineState)
        {
            sb.Append(input[bufferStart..]);
        }

        return sb.ToString().Trim();
    }
    #endregion
}
