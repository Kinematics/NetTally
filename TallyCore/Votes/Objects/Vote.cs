using System;
using System.Collections.Generic;
using System.Linq;
using NetTally.Extensions;
using NetTally.Utility;

namespace NetTally.Votes.Experiment
{
    public class Vote
    {
        public string FullText { get; }
        public List<string> Lines { get; } = new List<string>();

        public string TaskAndContent { get; private set; }
        public string Task { get; private set; }
        public int Rank { get; private set; }

        public VoteType VoteType { get; private set; }


        public Vote(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentNullException(nameof(input));

            FullText = input;

            ConfigVote(input);
        }


        private void ConfigVote(string input)
        {
            Lines.AddRange(input.GetStringLines());

            string firstLine = Lines.First();

            VoteString.GetVoteComponents(firstLine, out string prefix, out string marker, out string task, out string content);

            Task = task;

            if (int.TryParse(marker, out int rank))
            {
                Rank = rank;
                VoteType = VoteType.Rank;
            }
            else
            {
                Rank = 0;
                VoteType = VoteType.Vote;
            }

            if (string.IsNullOrEmpty(task))
            {
                TaskAndContent = content;
            }
            else
            {
                TaskAndContent = $"[{task}] {content}";
            }
        }




        public Vote ChangeTask(string task)
        {
            if (task == Task)
                return this;

            return new Vote(FullText) { Task = task };
        }


        public bool Match(string compare)
        {
            return Agnostic.StringComparer.Equals(compare, FullText);
        }







        public VoteLineSequence VoteLines { get; }

        public IEnumerable<VoteLineSequence> VoteBlocks
        {
            get
            {
                var voteBlocks = VoteLines.GroupAdjacentBySub(SelectSubLines, NonNullSelectSubLines);

                foreach (var block in voteBlocks)
                    yield return block as VoteLineSequence;
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
        private static VoteLine SelectSubLines(VoteLine line)
        {
            if (string.IsNullOrEmpty(line.Prefix))
                return line;

            return null;
        }

        /// <summary>
        /// Supplementary function for line grouping, in the event that the first
        /// line of the vote is indented (and thus would normally generate a null key).
        /// </summary>
        /// <param name="line">The line to generate a key for.</param>
        /// <returns>Returns the line, or "Key", as the key for a line.</returns>
        private static VoteLine NonNullSelectSubLines(VoteLine line) => line ?? VoteLine.Empty;

    }
}
