using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NetTally.Utility;

namespace NetTally.Votes
{
    public static class VoteString
    {
        private static class VoteComponents
        {
            public static string Prefix { get; } = "prefix";
            public static string Marker { get; } = "marker";
            public static string Task { get; } = "task";
            public static string Content { get; } = "content";
        }

        #region Vote regexes
        // Regex to get the different parts of the vote. Content includes only the first line of the vote.
        static readonly Regex voteLineRegex = new Regex(@"^(?<prefix>[-\s]*)\[\s*(?<marker>[xX✓✔1-9])\s*\]\s*(\[\s*(?![bui]\]|color=|url=)(?<task>[^]]*?)\])?\s*(?<content>.*)");
        // Single line version of the vote line regex.
        static readonly Regex voteLineRegexSingleLine = new Regex(@"^(?<prefix>[-\s]*)\[\s*(?<marker>[xX✓✔1-9])\s*\]\s*(\[\s*(?![bui]\]|color=|url=)(?<task>[^]]*?)\])?\s*(?<content>.*)", RegexOptions.Singleline);
        // Potential reference to another user's plan.
        static readonly Regex referenceNameRegex = new Regex(@"^(?<label>(?:\^|pin\b)(?=\s*\w)|(?:(?:base\s*)?plan\b)(?=\s*:?\s*\S))?\s*:?\s*(?<reference>.+)", RegexOptions.IgnoreCase);
        // Potential reference to another user's plan.
        static readonly Regex linkedReferenceRegex = new Regex(@"\[url=[^]]+\](.+)\[/url\]", RegexOptions.IgnoreCase);
        // Regex for extracting parts of the simplified condensed rank votes.
        static readonly Regex condensedVoteRegex = new Regex(@"^\[(?<task>[^]]*)\]\s*(?<content>.+)");

        // Check for a vote line that marks a portion of the user's post as an abstract base plan.
        static readonly Regex basePlanRegex = new Regex(@"base\s*plan((:|\s)+)(?<planname>.+)", RegexOptions.IgnoreCase);
        // Check for a plan reference. "Plan: Dwarf Raid"
        static readonly Regex anyPlanRegex = new Regex(@"^(base\s*)?plan(:|\s)+◈?(?<planname>.+)\.?$", RegexOptions.IgnoreCase);
        // Check for a plan reference, alternate format. "Arkatekt's Plan"
        static readonly Regex altPlanRegex = new Regex(@"^(?<planname>.+?'s\s+plan)$", RegexOptions.IgnoreCase);

        #endregion

        #region BBCode regexes
        // Regex to match any markup that we'll want to remove during comparisons.
        static readonly Regex markupRegex = new Regex(@"『/?[ibu]』|『color=[^』]+』|『/color』");

        // Regex for the pre-content area of a vote line, that will only match if there are no BBCode tags in that area of the vote line.
        static readonly Regex precontentRegex = new Regex(@"^(?:[\s-]*)\[\s*[xX✓✔1-9]\s*\]\s*(?!(?:(?:『/?[ibu]』|『color=[^』]+』|『/color』)\s*)+\[)\s*(\[[^]]+\])?");

        // Regex for any opening or closing BBCode tag.
        static readonly Regex allBBCodeRegex = new Regex(@"(『(?:/)?(?:b|i|u|color)(?(?<=『color)=[^』]+)』)");
        // Regex for any opening BBCode tag.
        static readonly Regex openBBCodeRegex = new Regex(@"^『(b|i|u|color)(?(?<=『color)=[^』]+)』");
        // Regex for any closing BBCode tag.
        static readonly Regex closeBBCodeRegex = new Regex(@"^『/(b|i|u|color)』");

        static readonly Dictionary<string, int> countTags = new Dictionary<string, int> {["b"] = 0,["i"] = 0,["u"] = 0,["color"] = 0 };
        #endregion

        #region Other regexes
        static readonly Regex extendedTextRegex = new Regex(@"(?<!\([^)]*)(((?<![pP][lL][aA][nN]\s*):(?!//))|—|(-(-+|\s+|\s*[^\p{Ll}])))");
        static readonly Regex extendedTextSentenceRegex = new Regex(@"(?<!\([^)]*)(?<![pP][lL][aA][nN]\b.+)(((?<=\S{4,})|(?<=\s[\p{Ll}]\S+))([.?!])(?:\s+[^\p{Ll}]))");
        static readonly Regex wordCountRegex = new Regex(@"\S+\b");
        #endregion

        #region Cleanup functions
        /// <summary>
        /// Remove BBCode markup from a text string.
        /// </summary>
        /// <param name="text">The text to clean up.</param>
        /// <returns>Returns the text without any BBCode markup.</returns>
        public static string RemoveBBCode(string text) => markupRegex.Replace(text, "").Trim();

        /// <summary>
        /// Remove URL BBCode from a vote line's content.
        /// </summary>
        /// <param name="contents">The contents of a vote line.</param>
        /// <returns>Returns the contents without any URL markup.</returns>
        public static string DeUrlContent(string contents)
        {
            string result = contents;

            Match m = linkedReferenceRegex.Match(contents);
            while (m.Success)
            {
                // (1: before)(2: [url=stuff] @?(3: inside) [/url])(4: after)
                string pattern = @"(.*?)(\[url=[^]]+\]@?(.+?)\[/url\])(.*)";
                string replacement = "$1$3$4";
                result = Regex.Replace(result, pattern, replacement);

                m = linkedReferenceRegex.Match(result);
            }

            return result;
        }

        /// <summary>
        /// Formats the provided line, converting any custom BBCode start/end characters (『』)
        /// into normal brackets ( [] ) for output.
        /// </summary>
        /// <param name="input">The input text.</param>
        /// <returns>Returns the input text line with BBCode characters converted to brackets.</returns>
        public static string FormatBBCodeForOutput(string input)
        {
            string output = input.Replace('『', '[');
            output = output.Replace('』', ']');

            return output;
        }

        /// <summary>
        /// Remove BBCode from the precontent area of a vote line, while leaving it in the content area.
        /// Unmatched tags are either removed (for extra ending tags) or closed (for extra opening tags).
        /// </summary>
        /// <param name="line">A vote line.</param>
        /// <returns>Returns the vote line cleaned up of BBCode.</returns>
        public static string CleanVoteLineBBCode(string line)
        {
            line = StripPrecontentBBCode(line);

            line = NormalizeContentBBCode(line);

            return line.Trim();
        }

        /// <summary>
        /// Remove all BBCode tags from the pre-content portion of a vote line.
        /// </summary>
        /// <param name="line">The vote line to modify.</param>
        /// <returns>Returns the vote line without any BBCode in the pre-content area.</returns>
        private static string StripPrecontentBBCode(string line)
        {
            string remainder = line ?? throw new ArgumentNullException(nameof(line));
            string piece = string.Empty;

            StringBuilder sb = new StringBuilder();

            // Pull out prefix and strip all BBCode from that section
            Regex prefix = new Regex(@"^([^[]*)(\[.+)");

            Match m = prefix.Match(remainder);

            if (m.Success)
            {
                piece = m.Groups[1].Value;
                remainder = m.Groups[2].Value;
            }

            piece = markupRegex.Replace(piece, "");

            sb.Append(piece);

            // Pull out marker and strip all BBCode from that section
            Regex marker = new Regex(@"^([^]]*])(.+)");

            m = marker.Match(remainder);

            if (m.Success)
            {
                piece = m.Groups[1].Value;
                remainder = m.Groups[2].Value;
            }

            piece = markupRegex.Replace(piece, "");

            sb.Append(piece);

            // Pull out task (if any) and strip all BBCode from that section
            // Leading spacing is stripped, so no spaces between [x] and [task].
            // Trailing spacing is stripped if it is inside BBCode.  
            // EG: [task] [/b]Content => [task]Content
            // EG: [task][/b] Content => [task] Content
            Regex task = new Regex(@"^(?:(?:\s*(?:『/?[ibu]』|『color=[^』]+』|『/color』))*\s*(\[[^]]*])(?:\s*(?:『/[ibu]』|『/color』))*)?(.+)");

            m = task.Match(remainder);

            if (m.Success)
            {
                if (m.Groups[1].Success)
                    piece = markupRegex.Replace(m.Groups[1].Value, "");
                else
                    piece = string.Empty;

                remainder = m.Groups[2].Value;
            }

            sb.Append(piece);
            sb.Append(remainder);

            return sb.ToString();
        }

        /// <summary>
        /// Make sure all BBCode tags have appropriate matching start/end tags.
        /// Any tags that don't have a proper match are removed.  This includes having
        /// a close tag before any opening tag (even if there's another open tag later on),
        /// or an open tag that's not followed by an end tag (even if there was an end tag
        /// earlier on).
        /// </summary>
        /// <param name="line">The vote line to modify.</param>
        /// <returns>Returns a normalized version of the vote line, with proper matching BBCode tags.</returns>
        private static string NormalizeContentBBCode(string line)
        {
            var lineSplit = allBBCodeRegex.Split(line);
            
            // If there were no BBCode tags, just return the original line.
            if (lineSplit.Length == 1)
                return line;

            // Run matches for opens and closes on all line splits, so we don't have to do it again later.
            Match[] openMatches = new Match[lineSplit.Length];
            Match[] closeMatches = new Match[lineSplit.Length];

            for (int i = 0; i < lineSplit.Length; i++)
            {
                openMatches[i] = openBBCodeRegex.Match(lineSplit[i]);
                closeMatches[i] = closeBBCodeRegex.Match(lineSplit[i]);
            }

            // Rebuild the result
            StringBuilder sb = new StringBuilder(line.Length);
            string tag;

            // Reset counts
            countTags["b"] = 0;
            countTags["i"] = 0;
            countTags["u"] = 0;
            countTags["color"] = 0;


            for (int i = 0; i < lineSplit.Length; i++)
            {
                // Skip blank entries from the split
                if (string.IsNullOrEmpty(lineSplit[i]))
                    continue;

                if (openMatches[i].Success)
                {
                    tag = openMatches[i].Groups[1].Value;

                    for (int j = i + 1; j < lineSplit.Length; j++)
                    {
                        if (string.IsNullOrEmpty(lineSplit[j]))
                            continue;

                        if (closeMatches[j].Success && closeMatches[j].Groups[1].Value == tag)
                        {
                            if (countTags[tag] > 0)
                            {
                                countTags[tag]--;
                            }
                            else
                            {
                                // We've found a matching open tag.  Add this close tag and end the loop.
                                sb.Append(lineSplit[i]);
                                break;
                            }
                        }
                        else if (openMatches[j].Success && openMatches[j].Groups[1].Value == tag)
                        {
                            countTags[tag]++;
                        }
                    }

                    countTags[tag] = 0;
                }
                else if (closeMatches[i].Success)
                {
                    tag = closeMatches[i].Groups[1].Value;

                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (string.IsNullOrEmpty(lineSplit[j]))
                            continue;

                        if (openMatches[j].Success && openMatches[j].Groups[1].Value == tag)
                        {
                            if (countTags[tag] > 0)
                            {
                                countTags[tag]--;
                            }
                            else
                            {
                                // We've found a matching open tag.  Add this close tag and end the loop.
                                sb.Append(lineSplit[i]);
                                break;
                            }
                        }
                        else if (closeMatches[j].Success && closeMatches[j].Groups[1].Value == tag)
                        {
                            countTags[tag]++;
                        }
                    }

                    countTags[tag] = 0;
                }
                else
                {
                    // This isn't a BBCode tag, so just add it to the pile.
                    sb.Append(lineSplit[i]);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Function that can modify vote lines that are read in according to
        /// preference specifications.
        /// </summary>
        /// <param name="line">The line as read.</param>
        /// <returns>Returns the vote line with any modifications.</returns>
        public static string ModifyLinesRead(string line)
        {
            string result = TrimExtendedTextDescription(line);

            return result;
        }

        /// <summary>
        /// A modification option that removes extended text descriptions
        /// from a vote line.  If a colon/dash/em-dash is found that separates
        /// less than 30% of the vote line length from more than 70%, the
        /// excess portion is dropped.
        /// </summary>
        /// <param name="voteLine">The original vote line.</param>
        /// <returns>Returns the vote line without the extended description.</returns>
        public static string TrimExtendedTextDescription(string voteLine)
        {
            if (string.IsNullOrEmpty(voteLine))
                return string.Empty;

            string lineContent = GetVoteContent(voteLine);

            // Get the offset into the original text that the content begins.
            int contentIndex = voteLine.IndexOf(lineContent, StringComparison.Ordinal);

            int trimmingIndex = GetTrimIndexForContent(lineContent);

            if (trimmingIndex > 0)
            {
                return voteLine.Substring(0, trimmingIndex + contentIndex);
            }

            return voteLine;
        }

        public static string TrimExtendedTextDescriptionOfContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            int trimmingIndex = GetTrimIndexForContent(content);

            if (trimmingIndex > 0)
            {
                return content.Substring(0, trimmingIndex);
            }

            return content;
        }

        /// <summary>
        /// Gets the index to trim from for a given content line.
        /// Determines the trim point as the last valid separation
        /// character that fits under the length limit.  If there are
        /// multiple separation points on the line, the untrimmed portion
        /// of the line must have more than one word in it.
        /// </summary>
        /// <param name="lineContent">Content of the vote line.</param>
        /// <returns>Returns the index that marks where to remove further text, or 0 if none.</returns>
        private static int GetTrimIndexForContent(string lineContent)
        {
            // If content is less than about 8 words long, don't try to trim it.
            if (lineContent.Length < 50)
                return 0;

            // The furthest into the content area that we're going to allow a
            // separator to be placed.
            int separatorLimit = lineContent.Length * 3 / 10;

            // Colons are always allowed as separators, though it needs
            // to run through a regex to be sure it's not part of a plan
            // definition line, or part of an absolute path on Windows.

            // Em dashes are always allowed as separators.

            // Search for any instances of hyphens in the content.
            // Only counts if there's a space after the hyphen, or if
            // the next word starts with a capital letter.

            // Select the one that comes closest to, without passing,
            // the separator limit.

            MatchCollection matches = extendedTextRegex.Matches(lineContent);

            // If there is only one separator, use it as long as it's within the limit.
            if (matches.Count == 1)
            {
                Match m = matches[0];
                if (m.Success && m.Index > 0 && m.Index < separatorLimit)
                {
                    return m.Index;
                }
            }
            // If there's more than one separator, take the last one that fits, but
            // only if there's more than one word before it.
            else if (matches.Count > 1)
            {
                for (int i = matches.Count-1; i >= 0; i--)
                {
                    Match m = matches[i];
                    if (m.Success && m.Index > 0 && m.Index < separatorLimit)
                    {
                        string partial = lineContent.Substring(0, m.Index);

                        if (CountWords(partial) > 1)
                            return m.Index;
                    }
                }
            }

            // Alternate trimming that reduces the vote to only the first sentence.
            matches = extendedTextSentenceRegex.Matches(lineContent);
            // Sentences may be taken up to half the line length.
            separatorLimit = lineContent.Length / 2;

            if (matches.Count > 0)
            {
                Match m = matches[0];
                if (m.Success && m.Index > 0 && m.Index < separatorLimit)
                {
                    return m.Index+1;
                }
            }

            // If no proper matches were found, return 0.
            return 0;
        }

        /// <summary>
        /// Counts the words in the provided string.
        /// </summary>
        /// <param name="partial">Part of a content line that we're going to count the words of.</param>
        /// <returns>Returns the number of words found in the provided string.</returns>
        private static int CountWords(string partial)
        {
            var matches = wordCountRegex.Matches(partial);
            return matches.Count;
        }
        #endregion

        #region 'Get' functions
        /// <summary>
        /// Get the requested element out of the cleaned version of the vote line.
        /// </summary>
        /// <param name="line">The vote line in question.</param>
        /// <param name="element">The element (regex name) to extract.</param>
        /// <returns>Returns the requested element of the vote line, or an empty string if not found.</returns>
        private static string GetVoteElement(string line, string element)
        {
            Match m = voteLineRegex.Match(line);
            if (m.Success)
            {
                return m.Groups[element]?.Value.Trim() ?? "";
            }

            return string.Empty;
        }

        /// <summary>
        /// Get the requested element out of the cleaned version of the condensed vote line.
        /// </summary>
        /// <param name="line">The condensed vote line in question.</param>
        /// <param name="element">The element (regex name) to extract.</param>
        /// <returns>Returns the requested element of the vote line, or an empty string if not found.</returns>
        private static string GetCondensedVoteElement(string line, string element)
        {
            Match m = condensedVoteRegex.Match(line);
            if (m.Success)
            {
                return m.Groups[element]?.Value.Trim() ?? "";
            }

            return string.Empty;
        }

        /// <summary>
        /// Get the prefix of the vote line.
        /// </summary>
        /// <param name="line">The vote line being examined.</param>
        /// <returns>Returns the prefix of the vote line.</returns>
        public static string GetVotePrefix(string line) => GetVoteElement(line, VoteComponents.Prefix).Replace(" ", string.Empty);

        /// <summary>
        /// Get the marker of the vote line.
        /// </summary>
        /// <param name="line">The vote line being examined.</param>
        /// <returns>Returns the marker of the vote line.</returns>
        public static string GetVoteMarker(string line) => GetVoteElement(line, VoteComponents.Marker);

        /// <summary>
        /// Get the (optional) task name from the provided vote line.
        /// If the vote type is specified to be Rank, use the regex that expects the condensed vote format.
        /// </summary>
        /// <param name="line">The vote line being examined.</param>
        /// <param name="voteType">Optional vote type.</param>
        /// <returns>Returns the name of the task, if found, or an empty string.</returns>
        public static string GetVoteTask(string line, VoteType voteType = VoteType.Vote)
        {
            if (voteType == VoteType.Rank)
            {
                return GetCondensedVoteElement(line, VoteComponents.Task);
            }

            return GetVoteElement(line, VoteComponents.Task);
        }

        /// <summary>
        /// Get the content of the vote line.
        /// An alternative function to get the content out of a vote that will attempt to
        /// use the condensed vote regex for rank votes.  If the vote type is not a ranked
        /// vote, it uses the default method.
        /// </summary>
        /// <param name="line">The vote text.</param>
        /// <param name="voteType">The type of vote.</param>
        /// <returns>Returns the content of this vote.</returns>
        public static string GetVoteContent(string line, VoteType voteType = VoteType.Vote)
        {
            if (voteType == VoteType.Rank)
            {
                return GetCondensedVoteElement(line, VoteComponents.Content);
            }

            return GetVoteElement(line, VoteComponents.Content);
        }

        /// <summary>
        /// Function to get all of the individual components of the vote line at once, including
        /// embedded BBCode (applied only to the content).
        /// </summary>
        /// <param name="line">The vote line to analyze.  Line must have already
        /// been processed by CleanVoteLineBBCode</param>
        /// <param name="prefix">The prefix (if any) for the vote line.</param>
        /// <param name="marker">The marker for the vote line.</param>
        /// <param name="task">The task (if any) for the vote line.</param>
        /// <param name="content">The content of the vote line.</param>
        public static void GetVoteComponents(string line,
            out string prefix, out string marker, out string task, out string content,
            bool byPartition = false)
        {
            Match m;
            if (byPartition)
                m = voteLineRegexSingleLine.Match(line);
            else
                m = voteLineRegex.Match(line);

            if (m.Success)
            {
                prefix = m.Groups[VoteComponents.Prefix].Value.Replace(" ", string.Empty);
                marker = m.Groups[VoteComponents.Marker].Value;
                task = m.Groups[VoteComponents.Task]?.Value.Trim() ?? "";
                content = m.Groups[VoteComponents.Content].Value;

                return;
            }
            
            throw new InvalidOperationException("Unable to parse components for vote line:\n"+line);
        }
        #endregion

        #region Misc functions
        /// <summary>
        /// Function to condense a rank vote to just the task and content of the original vote, for
        /// use in vote merging without needing to see all of the individual ranked votes.
        /// </summary>
        /// <param name="rankVote">The rank vote text.</param>
        /// <returns>Returns the vote condensed to just the [] task plus
        /// the vote content.  If there is no task, the [] is empty.</returns>
        public static string CondenseVote(string rankVote)
        {
            string prefix, marker, task, content;

            GetVoteComponents(rankVote, out prefix, out marker, out task, out content);

            return $"[{task}] {content}";
        }

        /// <summary>
        /// Get whether the vote line is a ranked vote line (ie: marker uses digits 1-9).
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns true if the vote marker is a digit, false if not.</returns>
        public static bool IsRankedVote(string voteLine)
        {
            string marker = GetVoteMarker(voteLine);

            if (string.IsNullOrEmpty(marker))
                return false;

            return char.IsDigit(marker, 0);
        }

        /// <summary>
        /// Get the potential names of vote plans from the contents of a vote line.
        /// It takes the original, the original plus the plan marker, the original
        /// with any trailing period stripped off, and that version plus the plan
        /// marker character. The last two are optional, depending on any trailing
        /// period.
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns possible plan names from the vote line.</returns>
        public static Dictionary<ReferenceType, List<string>> GetVoteReferenceNames(string voteLine)
        {
            string contents = GetVoteContent(voteLine);

            return GetVoteReferenceNamesFromContent(contents);
        }

        public static Dictionary<ReferenceType, List<string>> GetVoteReferenceNamesFromContent(string contents)
        {
            Dictionary<ReferenceType, List<string>> results = new Dictionary<ReferenceType, List<string>>();
            results[ReferenceType.Any] = new List<string>();
            results[ReferenceType.Plan] = new List<string>();
            results[ReferenceType.Voter] = new List<string>();
            results[ReferenceType.Label] = new List<string>();

            if (string.IsNullOrEmpty(contents))
                return results;

            contents = RemoveBBCode(contents);
            contents = DeUrlContent(contents);

            Match m = referenceNameRegex.Match(contents);
            if (m.Success)
            {
                string label = m.Groups["label"].Value.ToLowerInvariant();
                string name = m.Groups["reference"].Value;
                string pName = name.MakePlanName();

                if (m.Groups["label"].Success)
                {
                    // Some label: plan, base plan, ^, pin

                    results[ReferenceType.Label].Add(label);

                    // ^ can only be used for user proxies.
                    if (label == "^")
                    {
                        // [x] ^Kinematics => Kinematics
                        results[ReferenceType.Any].Add(name);
                        results[ReferenceType.Voter].Add(name);
                    }
                    // "Pin" can be used for user proxies, but also need to allow the
                    // entire contents to be stored as a plan name.
                    else if (label == "pin")
                    {
                        // [x] pin Kinematics => Kinematics
                        results[ReferenceType.Any].Add(name);
                        results[ReferenceType.Voter].Add(name);

                        // [x] Pin the tail on the donkey => ◈Pin the tail on the donkey
                        string pContent = contents.MakePlanName();
                        results[ReferenceType.Any].Add(pContent);
                        results[ReferenceType.Plan].Add(pContent);
                    }
                    else
                    {
                        // Other labels would include possible plan proxies or user proxies.

                        // [x] Plan Kinematics => Kinematics
                        // [x] Plan Boom. => Boom.
                        results[ReferenceType.Any].Add(name);
                        results[ReferenceType.Voter].Add(name);

                        // [x] Plan Kinematics => ◈Kinematics
                        // [x] Plan Boom. => ◈Boom.
                        results[ReferenceType.Any].Add(pName);
                        results[ReferenceType.Plan].Add(pName);
                    }
                }
                else
                {
                    // No labels.  Can apply to anything.

                    // [x] Kinematics => Kinematics
                    // [x] Boom. => Boom.
                    results[ReferenceType.Any].Add(name);
                    results[ReferenceType.Voter].Add(name);

                    // [x] Kinematics => ◈Kinematics
                    // [x] Boom. => ◈Boom.
                    results[ReferenceType.Any].Add(pName);
                    results[ReferenceType.Plan].Add(pName);
                }
            }

            return results;
        }


        /// <summary>
        /// Get the plan name from a vote line, if the vote line is formatted to define a plan.
        /// All BBCode is removed from the line, including URLs (such as @username markup).
        /// </summary>
        /// <param name="voteLine">The vote line being examined.  Cannot be null.</param>
        /// <param name="basePlan">Flag whether the vote line must be a "base plan", rather than any plan.</param>
        /// <returns>Returns the plan name, if found, or null if not.</returns>
        public static string GetPlanName(string voteLine, bool basePlan = false)
        {
            if (voteLine == null)
                throw new ArgumentNullException(nameof(voteLine));

            string lineContent = GetVoteContent(voteLine);
            string simpleContent = DeUrlContent(lineContent);

            Match m;

            // Base Plan XXX
            // Plan XXX
            if (basePlan)
                m = basePlanRegex.Match(simpleContent);
            else
                m = anyPlanRegex.Match(simpleContent);

            if (m.Success)
            {
                return m.Groups["planname"].Value.Trim();
            }

            if (!basePlan)
            {
                // Arkatekt's Plan
                m = altPlanRegex.Match(simpleContent);
                if (m.Success)
                {
                    return m.Groups["planname"].Value.Trim();
                }
            }

            return null;
        }

        /// <summary>
        /// Get the plan name from the provided vote line, and mark it with the plan name character
        /// marker if found.
        /// If no valid plan name is found, returns null.
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns the modified plan name, if found, or null if not.</returns>
        public static string GetMarkedPlanName(string voteLine)
        {
            string planname = GetPlanName(voteLine);
            if (planname != null)
                return planname.MakePlanName();

            return null;
        }

        #endregion

        #region Creating and modifying votes
        /// <summary>
        /// Create a vote line composed of the provided components.
        /// </summary>
        /// <param name="prefix">The prefix for the line.</param>
        /// <param name="marker">The marker for the line.</param>
        /// <param name="task">The task the line should be grouped with.</param>
        /// <param name="content">The contents of the vote.</param>
        /// <returns>Returns a complete vote line.</returns>
        public static string CreateVoteLine(string prefix, string marker, string task, string content)
        {
            if (prefix == null)
                prefix = "";

            if (string.IsNullOrEmpty(marker))
                marker = "X";

            if (task == null)
                task = "";

            if (string.IsNullOrEmpty(content))
                throw new ArgumentNullException(nameof(content));

            string line;

            if (string.IsNullOrEmpty(task))
                line = $"{prefix}[{marker}] {content}";
            else
                line = $"{prefix}[{marker}][{task}] {content}";

            return line;
        }

        /// <summary>
        /// Create a vote line composed of the provided components.
        /// Parameters should be null for values that won't change.
        /// </summary>
        /// <param name="prefix">The prefix for the line.</param>
        /// <param name="marker">The marker for the line.</param>
        /// <param name="task">The task the line should be grouped with.</param>
        /// <param name="content">The contents of the vote.</param>
        /// <returns>Returns a complete vote line.</returns>
        public static string ModifyVoteLine(string voteLine,
            string prefix = null, string marker = null, string task = null, string content = null,
            bool byPartition = false)
        {
            if (string.IsNullOrEmpty(voteLine))
                throw new ArgumentNullException(nameof(voteLine));

            // If all parameters are null, the vote line doesn't change.
            if (prefix == null && marker == null && task == null && content == null)
                return voteLine;

            string votePrefix;
            string voteMarker;
            string voteTask;
            string voteContent;

            // Use the original vote line value for any parameter that is null.
            GetVoteComponents(voteLine, out votePrefix, out voteMarker, out voteTask, out voteContent, byPartition: byPartition);

            prefix = prefix ?? votePrefix;
            marker = marker ?? voteMarker;
            task = task ?? voteTask;
            content = content ?? voteContent;

            string modifiedLine;

            if (string.IsNullOrEmpty(task))
                modifiedLine = $"{prefix}[{marker}] {content}";
            else
                modifiedLine = $"{prefix}[{marker}][{task}] {content}";

            return modifiedLine;
        }

        /// <summary>
        /// Replace the task in a vote line without modifying any BBCode markup.
        /// </summary>
        /// <param name="voteLine">The original vote line.</param>
        /// <param name="newTask">The new task to apply.
        /// Null or an empty string removes any existing task.</param>
        /// <param name="voteType">The type of vote being modified.</param>
        /// <returns>Returns the vote line with the task replaced, or the original
        /// string if the original line couldn't be matched by the regex.</returns>
        public static string ReplaceTask(string voteLine, string newTask, VoteType voteType = VoteType.Vote)
        {
            if (voteType == VoteType.Rank)
            {
                Match mc = condensedVoteRegex.Match(voteLine);

                if (mc.Success)
                {
                    return $"[{newTask ?? ""}] {mc.Groups[VoteComponents.Content].Value}";
                }
            }

            return ModifyVoteLine(voteLine, task: newTask ?? "", byPartition: true);
        }
        #endregion

    }
}
