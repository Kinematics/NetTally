using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NetTally.Utility;
using NetTally.Adapters;

namespace NetTally
{
    /// <summary>
    /// Class to hold an immutable rendition of a given post, including all of its
    /// vote-specific lines, for later comparison and re-use.
    /// </summary>
    public class PostComponents : IComparable, IComparer<PostComponents>
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
        readonly Regex tallyRegex = new Regex(@"^#####", RegexOptions.Multiline);
        // A valid vote line must start with [x] or -[x] (with any number of dashes).  It must be at the start of the line.
        readonly Regex voteLineRegex = new Regex(@"^[-\s]*\[\s*[xX+✓✔1-9]\s*\]");
        // Nomination-style votes.  @username, one per line.
        readonly Regex nominationLineRegex = new Regex(@"^\[url=""[^""]+?/members/\d+/""](?<username>@[^[]+)\[/url\]\s*$");

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
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            Author = author;
            ID = id;
            Text = text;
            Number = number;

            int idnum;
            if (int.TryParse(id, out idnum))
                IDValue = idnum;
            else
                IDValue = 0;

            if (IsTallyPost(text))
                return;

            var lines = Utility.Text.GetStringLines(text);
            var voteLines = lines.Where(a => voteLineRegex.Match(VoteString.RemoveBBCode(a)).Success);

            if (voteLines.Any())
            {
                VoteStrings = voteLines.Select(a => VoteString.CleanVoteLineBBCode(a)).ToList();

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
            if (startInfo.ByNumber)
            {
                return Number >= startInfo.Number;
            }

            return IDValue > startInfo.ID;
        }


        /// <summary>
        /// Get all plans from the current post.
        /// Plans named after a user are ignored as invalid.
        /// </summary>
        /// <returns>Returns a list of composed plans.</returns>
        public List<List<string>> GetAllPlansWithContent()
        {
            List<List<string>> results = new List<List<string>>();

            results.AddRange(BasePlans.Select(a => a.ToList()));

            if (VoteLines.Any())
            {
                var voteBlocks = VoteLines.GroupAdjacentBySub(SelectSubLines, NonNullSelectSubLines);

                foreach (var block in voteBlocks)
                {
                    if (block.Count() > 1)
                    {
                        string planname = VoteString.GetPlanName(block.Key);

                        if (planname != null && !VoteCounter.Instance.ReferenceVoters.ContainsAgnostic(planname))
                            results.Add(block.ToList());
                    }
                }
            }

            return results;
        }

        public List<List<string>> GetAllFullPostPlans()
        {
            List<List<string>> results = new List<List<string>>();

            if (VoteLines.Any())
            {
                var voteBlocks = VoteLines.GroupAdjacentBySub(SelectSubLines, NonNullSelectSubLines);

                if (!voteBlocks.Any(b => b.Count() > 1 && VoteString.GetPlanName(b.Key) != null))
                {
                    var firstLine = VoteLines.First();

                    string planname = VoteString.GetPlanName(firstLine);

                    if (planname != null && !VoteCounter.Instance.ReferenceVoters.ContainsAgnostic(planname))
                        results.Add(VoteLines);
                }
            }

            return results;
        }

        /// <summary>
        /// Set the WorkingVote list from a call to the supplied function.
        /// Reset Processed and ForceProcess flags.
        /// </summary>
        /// <param name="fn">A function that will generate a string list from the post components.</param>
        public void SetWorkingVote(Func<PostComponents, List<string>> fn)
        {
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
        public bool IsTallyPost(string postText)
        {
            // If the post contains the string "#####" at the start of the line for part of its text,
            // it's a tally post.
            string cleanText = VoteString.RemoveBBCode(postText);
            return (tallyRegex.Matches(cleanText).Count > 0);
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

            var voteBlocks = voteStrings.GroupAdjacentBySub(SelectSubLines, NonNullSelectSubLines);
            bool addBasePlans = true;

            foreach (var block in voteBlocks)
            {
                if (addBasePlans)
                {
                    if (block.Count() > 1)
                    {
                        string planName = VoteString.GetPlanName(block.Key, basePlan: true);

                        if (planName != null && VoteCounter.Instance.ReferenceVoters.ContainsAgnostic(planName) == false)
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
        }

        /// <summary>
        /// Utility function to determine whether adjacent lines should
        /// be grouped together.
        /// Creates a grouping key for the provided line.
        /// </summary>
        /// <param name="line">The line to check.</param>
        /// <returns>Returns the line as the key if it's not a sub-vote line.
        /// Otherwise returns null.</returns>
        private string SelectSubLines(string line)
        {
            string prefix = VoteString.GetVotePrefix(line);
            if (string.IsNullOrEmpty(prefix))
                return line;
            else
                return null;
        }

        /// <summary>
        /// Supplementary function for line grouping, in the event that the first
        /// line of the vote is indented (and thus would normally generate a null key).
        /// </summary>
        /// <param name="line">The line to generate a key for.</param>
        /// <returns>Returns the line, or "Key", as the key for a line.</returns>
        private string NonNullSelectSubLines(string line) => line ?? "Key";
        #endregion

        #region Compare Interface Functions
        /// <summary>
        /// IComparer function.
        /// </summary>
        /// <param name="x">The first object being compared.</param>
        /// <param name="y">The second object being compared.</param>
        /// <returns>Returns a negative value if x is 'before' y, 0 if they're equal, and
        /// a positive value if x is 'after' y.</returns>
        public int Compare(PostComponents x, PostComponents y)
        {
            if (x == null && y == null)
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;

            if (x.IDValue == 0 || y.IDValue == 0)
                return string.Compare(x.ID, y.ID, StringComparison.Ordinal);

            return x.IDValue - y.IDValue;
        }

        /// <summary>
        /// IComparable function.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Returns a negative value if this is 'before' y, 0 if they're equal, and
        /// a positive value if this is 'after' y.</returns>
        public int CompareTo(object obj) => Compare(this, obj as PostComponents);
        #endregion
    }
}
