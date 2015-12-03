using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NetTally.Utility;

namespace NetTally
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
        static readonly Regex referenceNameRegex = new Regex(@"^(?<label>(base\s*)?plan(:|\s)+)?(?<reference>.+)", RegexOptions.IgnoreCase);
        // Potential reference to another user's plan.
        static readonly Regex linkedReferenceRegex = new Regex(@"\[url=[^]]+\](.+)\[/url\]", RegexOptions.IgnoreCase);
        // Regex for extracting parts of the simplified condensed rank votes.
        static readonly Regex condensedVoteRegex = new Regex(@"^\[(?<task>[^]]*)\]\s*(?<content>.+)");

        // Check for a vote line that marks a portion of the user's post as an abstract base plan.
        static readonly Regex basePlanRegex = new Regex(@"base\s*plan((:|\s)+)(?<planname>.+)", RegexOptions.IgnoreCase);
        // Check for a plan reference.
        static readonly Regex anyPlanRegex = new Regex(@"^(base\s*)?plan(:|\s)+◈?(?<planname>.+)\.?$", RegexOptions.IgnoreCase);

        #endregion

        #region BBCode regexes
        // Regex to match any markup that we'll want to remove during comparisons.
        static readonly Regex markupRegex = new Regex(@"\[/?[ibu]\]|\[color[^]]*\]|\[/color\]");

        // Regex for the pre-content area of a vote line, that will only match if there are no BBCode tags in that area of the vote line.
        static readonly Regex precontentRegex = new Regex(@"^(?:[\s-]*)\[[xX✓✔1-9]\](?!\s*\[/(?:[bui]|color)\])(?!(?:\s*\[(?:[bui]|color=[^]]+)\])+\s*\[(?![bui]|color=[^]]+|url=[^]]+)[^]]+\])");

        // Regex for any opening BBCode tag.
        static readonly Regex openBBCodeRegex = new Regex(@"\[(b|i|u|color=[^]]+)\]");
        // Regex for any closing BBCode tag.
        static readonly Regex closeBBCodeRegex = new Regex(@"\[/(b|i|u|color)\]");
        // Regex to extract a tag from a BBCode element.
        static readonly Regex tagRegex = new Regex(@"\[/?(?<tag>[biu](?=\])|(?<=/)color(?=\])|color(?==[^]]+))(=[^]]+)?\]");
        // Regexes for the indicated closing tags.
        static readonly Dictionary<string, Regex> closeTagRegexes = new Dictionary<string, Regex>(StringComparer.OrdinalIgnoreCase)
        {
            ["b"] = new Regex(@"\[/b\]"),
            ["i"] = new Regex(@"\[/i\]"),
            ["u"] = new Regex(@"\[/u\]"),
            ["color"] = new Regex(@"\[/color\]"),
        };
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
                result = Regex.Replace(contents, pattern, replacement);

                m = linkedReferenceRegex.Match(result);
            }

            return result;
        }

        /// <summary>
        /// Remove BBCode from the precontent area of a vote line, while leaving it in the content area.
        /// Unmatched tags are either removed (for extra ending tags) or closed (for extra opening tags).
        /// </summary>
        /// <param name="line">A vote line.</param>
        /// <returns>Returns the vote line cleaned up of BBCode.</returns>
        public static string CleanVoteLineBBCode(string line)
        {
            Match m;
            string clean = line;

            m = precontentRegex.Match(line);
            while (m.Success == false)
            {
                string cleaned = markupRegex.Replace(clean, "", 1);

                if (cleaned != clean)
                {
                    clean = cleaned;
                    m = precontentRegex.Match(clean);
                }
                else
                {
                    break;
                }
            }

            var opens = openBBCodeRegex.Matches(clean);
            var closes = closeBBCodeRegex.Matches(clean);

            var openGroups = opens.OfType<Match>().GroupBy(a => GetTag(a.Value));
            var closeGroups = closes.OfType<Match>().GroupBy(a => GetTag(a.Value));

            foreach (var tag in closeGroups)
            {
                var matchingOpen = openGroups.FirstOrDefault(a => a.Key == tag.Key);
                // If there are no matching opens for the closing tag, remove all instances of the closing tag.
                if (matchingOpen == null)
                {
                    clean = closeTagRegexes[tag.Key].Replace(clean, "", tag.Count());
                }
                else
                {
                    // Otherwise remove as many additional close tags as are found.
                    int diff = tag.Count() - matchingOpen.Count();

                    if (diff > 0)
                    {
                        clean = closeTagRegexes[tag.Key].Replace(clean, "", diff);
                    }
                }
            }

            foreach (var tag in openGroups)
            {
                var matchingClose = closeGroups.FirstOrDefault(a => a.Key == tag.Key);

                int? diff = tag.Count() - matchingClose?.Count();
                string closeTag = $"[/{tag.Key}]";

                int num = diff.HasValue ? diff.Value : tag.Count();

                for (int i = 0; i < num; i++)
                {
                    clean += closeTag;
                }
            }

            return clean.Trim();
        }

        /// <summary>
        /// Tags a BBCode opening or closing tag element, and extracts the tag from it.
        /// </summary>
        /// <param name="input">A BBCode markup element.</param>
        /// <returns>Returns the BBCode tag from the element.</returns>
        private static string GetTag(string input)
        {
            Match m = tagRegex.Match(input);
            if (m.Success)
                return m.Groups["tag"].Value;

            return string.Empty;
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
            bool ByPartition = false)
        {
            Match m;
            if (ByPartition)
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

            if (marker == string.Empty)
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

            contents = RemoveBBCode(contents);
            contents = DeUrlContent(contents);

            Match m2 = referenceNameRegex.Match(contents);
            if (m2.Success)
            {
                string label = m2.Groups["label"].Value;
                if (!string.IsNullOrEmpty(label))
                    results[ReferenceType.Label].Add(label);

                string name = m2.Groups["reference"].Value;
                string pName = $"{Text.PlanNameMarker}{name}";

                // [x] Plan Kinematics => Kinematics
                // [x] Plan Boom. => Boom.
                results[ReferenceType.Any].Add(name);
                results[ReferenceType.Voter].Add(name);

                // [x] Plan Kinematics => ◈Kinematics
                // [x] Plan Boom. => ◈Boom.
                results[ReferenceType.Any].Add(pName);
                results[ReferenceType.Plan].Add(pName);

                // [x] Plan Kinematics. => Kinematics
                // [x] Plan Boom. => Boom
                // [x] Plan Kinematics. => ◈Kinematics
                // [x] Plan Boom. => ◈Boom
                if (name.EndsWith("."))
                {
                    name = name.Substring(0, name.Length - 1);
                    pName = $"{Text.PlanNameMarker}{name}";

                    results[ReferenceType.Any].Add(name);
                    results[ReferenceType.Voter].Add(name);
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

            if (basePlan)
                m = basePlanRegex.Match(simpleContent);
            else
                m = anyPlanRegex.Match(simpleContent);

            if (m.Success)
            {
                return m.Groups["planname"].Value.Trim();
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
                return Text.PlanNameMarker + planname;

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

            if (marker == null || marker == string.Empty)
                marker = "X";

            if (task == null)
                task = "";

            if (string.IsNullOrEmpty(content))
                throw new ArgumentNullException(nameof(content));

            string line;

            if (task == string.Empty)
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
            bool ByPartition = false)
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
            GetVoteComponents(voteLine, out votePrefix, out voteMarker, out voteTask, out voteContent, ByPartition: ByPartition);

            prefix = prefix ?? votePrefix;
            marker = marker ?? voteMarker;
            task = task ?? voteTask;
            content = content ?? voteContent;

            string modifiedLine;

            if (task == string.Empty)
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

            return ModifyVoteLine(voteLine, task: newTask ?? "", ByPartition: true);
        }
        #endregion

    }
}
