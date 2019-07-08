using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NetTally.Utility;
using NetTally.Votes;

namespace NetTally.Experiment3
{
    /// <summary>
    /// Class to hold relevent post information when read from the forum.
    /// </summary>
    public class Post : IComparable<Post>, IEquatable<Post>, IComparable
    {
        /// <summary>
        /// The author of the post.
        /// </summary>
        public string Author { get; }
        /// <summary>
        /// The thread post number of the post.
        /// </summary>
        public int Number { get; }
        /// <summary>
        /// The unique ID of the post.
        /// </summary>
        public string ID { get; }
        /// <summary>
        /// The unique ID of the post as an integer.
        /// </summary>
        public int IDValue { get; }
        /// <summary>
        /// The text of the post.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Flag whether or not this post contains vote information.
        /// </summary>
        public bool IsVote => VoteLines.Count > 0;
        /// <summary>
        /// Any vote lines found in the post's text.
        /// </summary>
        public IReadOnlyList<VoteLine> VoteLines { get; }
        /// <summary>
        /// Vote lines with base/proposed plans removed.
        /// </summary>
        public List<VoteLine> WorkingVoteLines { get; } = new List<VoteLine>();

        // A post with ##### at the start of one of the lines is a posting of tally results.  Don't read it.
        readonly static Regex tallyRegex = new Regex(@"^#####", RegexOptions.Multiline);

        public bool Processed { get; set; }
        public bool ForceProcess { get; set; }


        /// <summary>
        /// Constructor for the a new post.
        /// Stores the post data, and extracts any vote lines if this isn't a tally post.
        /// </summary>
        /// <param name="author">The author of the post.</param>
        /// <param name="postId">The ID of the post.</param>
        /// <param name="text">The text of the post.</param>
        /// <param name="number">The thread post number.</param>
        public Post(string author, string postId, string text, int number = 0, IQuest? quest = null)
        {
            Author = author ?? throw new ArgumentNullException(nameof(author));
            ID = postId ?? throw new ArgumentNullException(nameof(postId));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Number = number;

            int.TryParse(postId, out int idnum);
            if (idnum < 0)
                throw new ArgumentOutOfRangeException($"Post ID is negative ({postId})");
            IDValue = idnum;

            VoteLines = GetPostAnalysisResults(Text);
        }

        /// <summary>
        /// Get the list of all found vote lines in the post's text content.
        /// If no normal votes are found, also check if this is a nomination post.
        /// </summary>
        /// <param name="text">Text of the post.</param>
        /// <returns>Returns a readonly list of any vote lines found.</returns>
        private IReadOnlyList<VoteLine> GetPostAnalysisResults(string text)
        {
            List<VoteLine> results = new List<VoteLine>();

            if (!IsTallyPost())
            {
                var postTextLines = text.GetStringLines();

                results.AddRange(GetVoteLines(postTextLines));

                if (results.Count == 0)
                {
                    results.AddRange(GetNominationVoteLines(postTextLines));
                }
            }

            return results;
        }

        /// <summary>
        /// Determine if the provided post text is someone posting the results of a tally.
        /// </summary>
        /// <param name="postText">The text of the post to check.</param>
        /// <returns>Returns true if the post contains tally results.</returns>
        bool IsTallyPost()
        {
            // If the post contains the string "#####" at the start of the line for part of its text,
            // it's a tally post.
            string cleanText = VoteLineParser.StripBBCode(Text);
            return (tallyRegex.Matches(cleanText).Count > 0);
        }

        /// <summary>
        /// Extracts vote lines from the text lines of the post.
        /// </summary>
        /// <param name="postTextLines">The lines from the post.</param>
        /// <returns>Returns an enumerable of all the vote lines found in the post text.</returns>
        private IEnumerable<VoteLine> GetVoteLines(List<string> postTextLines)
        {
            foreach (var line in postTextLines)
            {
                var voteLine = VoteLineParser.ParseLine(line);

                if (voteLine != null)
                    yield return voteLine;
            }
        }

        readonly static Regex nominationLineRegex = new Regex(@"^『url=""[^""]+?/members/\d+/""』@?(?<username>[^『]+)『/url』\s*$");
        /// <summary>
        /// Examine the post to see if it qualifies as a nomination post.
        /// If so, return the nomination lines formatted as votes.
        /// </summary>
        /// <param name="postTextLines">The set of lines from the post.</param>
        /// <returns>Returns the list of nomination votes, or an empty list.</returns>
        private List<VoteLine> GetNominationVoteLines(List<string> postTextLines)
        {
            List<VoteLine> results = new List<VoteLine>();

            foreach (var line in postTextLines)
            {
                Match m = nominationLineRegex.Match(line);
                if (m.Success)
                {
                    VoteLine voteLine = new VoteLine("", "X", "", m.Groups["username"].Value, MarkerType.Vote, 100);
                    results.Add(voteLine);
                }
                else
                {
                    results.Clear();
                    return results;
                }
            }

            return results;
        }


#nullable disable
        public static int Compare(Post left, Post right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (left is null)
                return -1;
            if (right is null)
                return 1;

            if (left.IDValue == 0 || right.IDValue == 0)
                return string.Compare(left.ID, right.ID, StringComparison.Ordinal);

            return left.IDValue.CompareTo(right.IDValue);
        }

        public int CompareTo(Post other)
        {
            return Compare(this, other);
        }

        public int CompareTo(object obj)
        {
            return Compare(this, obj as Post);
        }

        public bool Equals(Post other)
        {
            return Compare(this, other) == 0;
        }

        public override bool Equals(object obj)
        {
            return Compare(this, obj as Post) == 0;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public static bool operator >(Post first, Post second) => Compare(first, second) == 1;
        public static bool operator <(Post first, Post second) => Compare(first, second) == -1;
        public static bool operator >=(Post first, Post second) => Compare(first, second) >= 0;
        public static bool operator <=(Post first, Post second) => Compare(first, second) <= 0;
        public static bool operator ==(Post first, Post second) => Compare(first, second) == 0;
        public static bool operator !=(Post first, Post second) => Compare(first, second) != 0;
#nullable enable
    }
}
