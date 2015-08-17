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
        public List<PostLine> PostLines { get; }
        public List<PostLine> VoteLines { get; }
        //public List<string> VoteStrings { get; }

        // Indicate whether this post contains a vote of any sort.
        public bool IsVote => VoteLines != null && VoteLines.Count > 0;

        // Regex to extract out all individual lines from a post's text.
        readonly Regex allLinesRegex = new Regex(@"^.+$", RegexOptions.Multiline);
        // A post with ##### at the start of one of the lines is a posting of tally results.  Don't read it.
        readonly Regex tallyRegex = new Regex(@"^#####");
        // A valid vote line must start with [x] or -[x] (with any number of dashes).  It must be at the start of the line.
        readonly Regex voteLineRegex = new Regex(@"^[-\s]*\[\s*[xX+✓✔1-9]\s*\]");
        // Nomination-style votes.  @username, one per line.
        readonly Regex nominationLineRegex = new Regex(@"^\[url=""[^""]+?/members/\d+/""](?<username>@[^[]+)\[/url\]");


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="author">The author of the post.</param>
        /// <param name="id">The ID (string) of the post.</param>
        /// <param name="text">The raw text contents of the post.</param>
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

            PostLines = GetPostLines(text);

            if (IsTallyPost())
                return;

            VoteLines = GetVoteLines();
        }

        /// <summary>
        /// Convert the provided match results into a list of post lines for ease of use.
        /// </summary>
        /// <param name="matches">A collection of regex matches.</param>
        /// <returns>Returns the list of post lines contained by the regex matches.</returns>
        public List<PostLine> GetPostLines(string text)
        {
            // Pull all individual lines
            MatchCollection matches = allLinesRegex.Matches(text);

            // Keep only non-whitespace lines (saved as a PostLine)
            var lines = from Match m in matches
                        let line = m.Value.Trim()
                        where line != string.Empty
                        select new PostLine(line);

            return lines.ToList();
        }

        /// <summary>
        /// Get all the vote lines out of the post lines in the post.
        /// Nomination-style lines are selected if all post lines validate as nomination lines.
        /// </summary>
        /// <returns>Returns a list of all the PostLines in the post that are
        /// valid vote lines.</returns>
        public List<PostLine> GetVoteLines()
        {
            var lines = from l in PostLines
                        where voteLineRegex.Match(l.Clean).Success
                        select l;

            if (!lines.Any())
            {
                if (PostLines.All(l => nominationLineRegex.Match(l.Clean).Success))
                {
                    lines = from l in PostLines
                            select new PostLine("[X] " + l.Original);
                }
            }

            return lines.ToList();
        }

        /// <summary>
        /// Determine if the provided post text is someone posting the results of a tally.
        /// </summary>
        /// <returns>Returns true if the post contains tally results.</returns>
        public bool IsTallyPost()
        {
            // A tally post is marked by the string "#####" at the start of a line.
            // If the clean view of any of the post's lines matches the regex, it's a tally post.
            return PostLines.Any(p => tallyRegex.Match(p.Clean).Success);
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
