using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NetTally.Extensions;
using NetTally.Forums;
using NetTally.Utility;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally
{
    /// <summary>
    /// Class to hold an immutable rendition of a given post, including all of its
    /// vote-specific lines, for later comparison and re-use.
    /// </summary>
    public class PostComponents : IComparable, IComparable<PostComponents>
    {
        public string Author { get; }
        public int Number { get; }
        public string ID { get; }
        public string Text { get; }
        public int IDValue { get; }
        public List<string> VoteStrings { get; }
        public List<IGrouping<string, string>> BasePlans { get; private set; }
        public List<string> VoteLines { get; private set; }
        public List<string> RankLines { get; private set; }

        public List<string> WorkingVote { get; private set; }
        public bool Processed { get; set; }
        public bool ForceProcess { get; set; }

        // Indicate whether this post contains a vote of any sort.
        public bool IsVote => VoteStrings != null && VoteStrings.Count > 0;

        // A post with ##### at the start of one of the lines is a posting of tally results.  Don't read it.
        readonly static Regex tallyRegex = new Regex(@"^#####", RegexOptions.Multiline);
        // A valid vote line must start with [x] or -[x] (with any number of dashes).  It must be at the start of the line.
        readonly static Regex voteLineRegex = new Regex(@"^[-\s]*\[\s*[xX✓✔1-9]\s*\]");
        // Nomination-style votes.  @username, one per line.
        readonly static Regex nominationLineRegex = new Regex(@"^\[url=""[^""]+?/members/\d+/""](?<username>@[^[]+)\[/url\]\s*$");

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="author">The author of the post.</param>
        /// <param name="id">The ID (string) of the post.</param>
        /// <param name="text">The text contents of the post.</param>
        public PostComponents(string author, string id, string text, int number = 0)
        {
            if (string.IsNullOrEmpty(author))
                throw new ArgumentNullException(nameof(author));
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));

            Text = text ?? throw new ArgumentNullException(nameof(text));
            Author = author;
            ID = id;
            Number = number;

            if (int.TryParse(id, out int idnum))
                IDValue = idnum;
            else
                IDValue = 0;

            if (IsTallyPost(text))
                return;

            var lines = text.GetStringLines();
            var voteLines = lines.Where(a => voteLineRegex.Match(VoteString.RemoveBBCode(a)).Success);

            if (voteLines.Any())
            {
                VoteStrings = voteLines.Select(a => VoteString.CleanVoteLineBBCode(a))
                    .Select(a => VoteString.ModifyLinesRead(a))
                    .Select(a => VoteString.CleanVoteLineBBCode(a)).ToList();

                SeparateVoteStrings(VoteStrings);
            }
            else if (lines.All(a => nominationLineRegex.Match(a).Success))
            {
                VoteStrings = lines.Select(a => "[X] " + a.Trim()).ToList();

                SeparateVoteStrings(VoteStrings);
            }
        }

        /// <summary>
        /// Checks whether the current post is 'after' the starting point that
        /// is documented in the start info object.
        /// </summary>
        /// <param name="startInfo">The information about where the tally starts.</param>
        /// <returns>Returns true if this post comes after the defined starting point.</returns>
        public bool IsAfterStart(ThreadRangeInfo startInfo)
        {
            if (startInfo == null)
                throw new ArgumentNullException(nameof(startInfo));

            if (startInfo.ByNumber)
            {
                return Number >= startInfo.Number;
            }

            return IDValue > startInfo.ID;
        }

        /// <summary>
        /// Set the WorkingVote list from a call to the supplied function.
        /// Reset Processed and ForceProcess flags.
        /// </summary>
        /// <param name="fn">A function that will generate a string list from the post components.</param>
        public void SetWorkingVote(Func<PostComponents, List<string>> fn)
        {
            if (fn == null)
                throw new ArgumentNullException(nameof(fn));

            WorkingVote = fn(this);
            Processed = false;
            ForceProcess = false;
        }

        #region Private utility construction functions
        /// <summary>
        /// Determine if the provided post text is someone posting the results of a tally.
        /// </summary>
        /// <param name="postText">The text of the post to check.</param>
        /// <returns>Returns true if the post contains tally results.</returns>
        public static bool IsTallyPost(string postText)
        {
            // If the post contains the string "#####" at the start of the line for part of its text,
            // it's a tally post.
            string cleanText = VoteString.RemoveBBCode(postText);
            return (tallyRegex.Matches(cleanText).Count > 0);
            // 6.6 ms

            //readonly static Regex tallyRegex2 = new Regex(@"^(\[/?(b|i|u|color(=[^]]+)?)\])*#####", RegexOptions.Multiline);
            //return (tallyRegex2.Matches(postText).Count > 0);
            // 9.0 ms

            //readonly static Regex tallyRegex3 = new Regex(@"^(\[/?(b|i|u|color(=[^]]+)?)\])*#####");
            //var lines = StringUtility.GetStringLines(postText);
            //return lines.Any(a => tallyRegex3.Match(a).Success);
            // 7.2 ms

            //readonly static Regex tallyRegex4 = new Regex(@"^(\[color=[^]]+\])*#####");
            //var lines = StringUtility.GetStringLines(postText);
            //return lines.Any(a => tallyRegex4.Match(a).Success);
            // 7.4 ms
        }

        /// <summary>
        /// Takes the full vote string list of the vote and breaks it
        /// into base plans, regular vote lines, and ranked vote lines.
        /// Store in the local object properties.
        /// </summary>
        /// <param name="voteStrings">The list of all the lines in the vote post.</param>
        private void SeparateVoteStrings(List<string> voteStrings)
        {
            BasePlans = new List<IGrouping<string, string>>();
            List<string> consolidatedLines = new List<string>();

            // Group blocks based on parent vote lines (no prefix).
            // Key for each block is the parent vote line.
            var voteBlocks = voteStrings.GroupAdjacentToPreviousKey(
                (s) => string.IsNullOrEmpty(VoteString.GetVotePrefix(s)),
                (s) => s,
                (s) => s);

            bool addBasePlans = true;

            foreach (var block in voteBlocks)
            {
                if (addBasePlans)
                {
                    if (block.Count() > 1)
                    {
                        string planName = VoteString.GetPlanName(block.Key, basePlan: true);

                        if (planName != null && !VoteCounter.Instance.ReferenceVoters.Contains(planName, Agnostic.StringComparer))
                        {
                            BasePlans.Add(block);
                            continue;
                        }
                    }
                }
                addBasePlans = false;

                consolidatedLines.AddRange(block.ToList());
            }

            RankLines = new List<string>();
            VoteLines = new List<string>();

            foreach (var line in consolidatedLines)
            {
                if (VoteString.IsRankedVote(line))
                    RankLines.Add(line);
                else
                    VoteLines.Add(line);
            }

            // If we have ranked vote options, make sure we don't have duplicate entries,
            // or the same option voted on different ranks.
            if (RankLines.Count > 0)
            {
                var groupRankLinesMulti = RankLines.GroupBy(line => VoteString.GetVoteContent(line), Agnostic.StringComparer)
                    .Where(group => group.Count() > 1);

                // If there are any, remove all but the top ranked option.
                foreach (var lineGroup in groupRankLinesMulti)
                {
                    var topOption = lineGroup.MinObject(a => VoteString.GetVoteMarker(a));
                    var otherOptions = lineGroup.Where(a => a != topOption).ToList();

                    foreach (string otherOption in otherOptions)
                    {
                        RankLines.Remove(otherOption);
                    }
                }
            }
        }
        #endregion

        #region Compare Interface Functions

        /// <summary>
        /// IComparable function.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Returns a negative value if this is 'before' obj, 0 if they're equal, and
        /// a positive value if this is 'after' obj.</returns>
        public int CompareTo(object obj) => Compare(this, obj as PostComponents);

        public int CompareTo(PostComponents other) => Compare(this, other);

        public override bool Equals(object obj)
        {
            if (obj is PostComponents other)
            {
                return CompareTo(other) == 0;
            }

            return false;
        }

        public override int GetHashCode() => Author.GetHashCode() ^ IDValue.GetHashCode();

        /// <summary>
        /// IComparer function.
        /// </summary>
        /// <param name="left">The first object being compared.</param>
        /// <param name="right">The second object being compared.</param>
        /// <returns>Returns a negative value if left is 'before' right, 0 if they're equal, and
        /// a positive value if left is 'after' right.</returns>
        public static int Compare(PostComponents left, PostComponents right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (ReferenceEquals(left, null))
                return -1;
            if (ReferenceEquals(right, null))
                return 1;

            if (left.IDValue == 0 || right.IDValue == 0)
                return string.Compare(left.ID, right.ID, StringComparison.Ordinal);

            return left.IDValue - right.IDValue;
        }

        public static bool operator ==(PostComponents left, PostComponents right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }

        public static bool operator !=(PostComponents left, PostComponents right) => !(left == right);

        public static bool operator <(PostComponents left, PostComponents right) => (Compare(left, right) < 0);

        public static bool operator >(PostComponents left, PostComponents right) => (Compare(left, right) > 0);
        #endregion
    }
}
