using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetTally
{
    /// <summary>
    /// Class to hold an immutable rendition of a given post, including all of its
    /// vote-specific lines, for later comparison and re-use.
    /// </summary>
    public class PostComponents : IComparable, IComparer<PostComponents>
    {
        public string Author { get; }
        public string ID { get; }
        public string Text { get; }
        public int IDValue { get; }
        public List<string> VoteStrings { get; }

        // Indicate whether this post contains a vote of any sort.
        public bool IsVote => VoteStrings != null && VoteStrings.Count > 0;

        // A post with ##### at the start of one of the lines is a posting of tally results.  Don't read it.
        readonly Regex tallyRegex = new Regex(@"^#####", RegexOptions.Multiline);
        // A valid vote line must start with [x] or -[x] (with any number of dashes).  It must be at the start of the line.
        readonly Regex allVoteRegex = new Regex(@"^(\s|\[/?[ibu]\]|\[color[^]]+\])*-*\s*\[\s*[xX+✓✔1-9]\s*\].*", RegexOptions.Multiline);
        // Nomination-style votes.  @username, one per line.
        readonly Regex nominationRegex = new Regex(@"^\[url=""[^""]+?/members/\d+/""](?<username>@[^[]+)\[/url\]\s*(?=[\r\n]|$)", RegexOptions.Multiline);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="author">The author of the post.</param>
        /// <param name="id">The ID (string) of the post.</param>
        /// <param name="text">The text contents of the post.</param>
        public PostComponents(string author, string id, string text)
        {
            Author = author;
            ID = id;
            Text = text;

            int idnum;
            if (int.TryParse(id, out idnum))
                IDValue = idnum;
            else
                IDValue = 0;

            if (IsTallyPost(text))
                return;

            MatchCollection matches = allVoteRegex.Matches(text);

            if (matches.Count > 0)
            {
                VoteStrings = GetMatchStrings(matches);
            }
            else
            {
                matches = nominationRegex.Matches(text);
                if (matches.Count > 0)
                {
                    VoteStrings = GetNominationStrings(matches);
                }
            }
        }

        /// <summary>
        /// Determine if the provided post text is someone posting the results of a tally.
        /// </summary>
        /// <param name="postText">The text of the post to check.</param>
        /// <returns>Returns true if the post contains tally results.</returns>
        public bool IsTallyPost(string postText)
        {
            // If the post contains the string "#####" at the start of the line for part of its text,
            // it's a tally post.
            string cleanText = VoteString.CleanVote(postText);
            return (tallyRegex.Matches(cleanText).Count > 0);
        }

        /// <summary>
        /// Convert the provided match results into a list of strings for ease of use.
        /// </summary>
        /// <param name="matches">A collection of regex matches.</param>
        /// <returns>Returns the list of strings contained by the regex matches.</returns>
        public List<string> GetMatchStrings(MatchCollection matches)
        {
            var strings = from Match m in matches
                          select m.Value.Trim();

            return strings.ToList();
        }

        /// <summary>
        /// Convert the provided match results for user nominations into a list of
        /// vote strings for ease of use.
        /// </summary>
        /// <param name="matches">A collection of regex matches.</param>
        /// <returns>Returns the list of strings contained by the regex matches.</returns>
        private List<string> GetNominationStrings(MatchCollection matches)
        {
            var strings = from Match m in matches
                          select "[X] " + m.Value.Trim();

            return strings.ToList();
        }

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
                return string.Compare(x.ID, y.ID);

            return x.IDValue - y.IDValue;
        }

        /// <summary>
        /// IComparable function.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Returns a negative value if this is 'before' y, 0 if they're equal, and
        /// a positive value if this is 'after' y.</returns>
        public int CompareTo(object obj)
        {
            return Compare(this, obj as PostComponents);
        }
    }
}
