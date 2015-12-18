using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NetTally.Output
{
    /// <summary>
    /// Meta-vote class to allow storing the primary vote, along with any
    /// 'child' votes that have the same first line of text as the main vote.
    /// </summary>
    public class VoteNode
    {
        readonly VoteNode Parent;

        public string Text { get; set; }

        public List<VoteNode> Children { get; } = new List<VoteNode>();

        public HashSet<string> Voters { get; } = new HashSet<string>();
        public HashSet<string> AllVoters { get; } = new HashSet<string>();

        public int VoterCount => AllVoters.Count(v => !v.StartsWith(Utility.Text.PlanNameMarker, StringComparison.Ordinal));

        bool HasParent => Parent != null;
        bool HasChildren => Children.Count > 0;

        public VoteNode(string text, HashSet<string> voters)
            : this(text, voters, null)
        {
        }

        private VoteNode(string text, HashSet<string> voters, VoteNode parent)
        {
            Parent = parent;
            Text = text;
            AddVoters(voters);
        }

        /// <summary>
        /// Add new voters to this node's voter list.
        /// </summary>
        /// <param name="voters">Voters to add.</param>
        public void AddVoters(HashSet<string> voters)
        {
            if (voters != null)
            {
                Voters.UnionWith(voters);
                AllVoters.UnionWith(voters);
            }
        }

        /// <summary>
        /// Add a child node to the current node.
        /// Update this node's voter list to include any new voters from the child.
        /// </summary>
        /// <param name="text">Text of the child node.</param>
        /// <param name="voters">Voters for the child node.</param>
        public void AddChild(string text, HashSet<string> voters)
        {
            VoteNode child = new VoteNode(text, voters, this);
            Children.Add(child);
            AllVoters.UnionWith(child.Voters);
        }

        /// <summary>
        /// Function to get a line for one of the compact mode displays,
        /// based on whether this is a parent or child node.
        /// </summary>
        /// <param name="displayMode">The display mode currently being used.</param>
        /// <returns>Returns a string representation of the current vote node.</returns>
        public string GetLine(DisplayMode displayMode)
        {
            var lines = Utility.Text.GetStringLines(Text);
            if (lines.Count == 0)
                return string.Empty;

            string results = string.Empty;

            string planname = VoteString.GetPlanName(lines[0]);
            bool isPlan = planname != null && VoteCounter.Instance.HasPlan(planname);


            if (HasChildren || (!HasParent && lines.Count == 1))
            {
                // Parent node, or solitary node with 1 line.
                results = VoteString.ModifyVoteLine(lines[0], marker: VoterCount.ToString());
            }
            else if (HasParent && lines.Count == 1)
            {
                // Child node with 1 line
                if (displayMode == DisplayMode.Compact)
                {
                    results = VoteString.ModifyVoteLine(lines[0], prefix: "", marker: VoterCount.ToString());
                }
                else
                {
                    results = VoteString.ModifyVoteLine(lines[0], prefix: "-", marker: VoterCount.ToString());
                }
            }
            else if (!HasChildren)
            {
                // Other nodes without children (typically child nodes).

                StringBuilder sb = new StringBuilder();

                if (HasParent && displayMode == DisplayMode.CompactNoVoters)
                    sb.Append("-");

                sb.Append("[");
                sb.Append(VoterCount);
                sb.Append("]");

                // Only explicitly add tasks to parent nodes
                if (!HasParent)
                {
                    string task = VoteString.GetVoteTask(lines[0]);
                    if (task != string.Empty)
                        sb.Append($"[{task}]");
                }


                sb.Append(" Plan: ");
                string firstVoter = VoteInfo.GetFirstVoter(Voters);
                sb.Append(firstVoter);

                // Only add the link if we're not showing the voters
                if (displayMode == DisplayMode.CompactNoVoters)
                {
                    string link;

                    VoteType voteType = Utility.Text.IsPlanName(firstVoter) ? VoteType.Plan : VoteType.Vote;
                    link = VoteInfo.GetVoterUrl(firstVoter, voteType);

                    sb.Append(" — ");
                    sb.Append(link);
                }

                results = sb.ToString();
            }

            // Child nodes in compact mode will be put in spoilers.
            // Remove BBCode and change the [10] count to (10).
            if (HasParent && displayMode == DisplayMode.Compact)
            {
                string cleanString = VoteString.RemoveBBCode(results);

                Regex bracketRegex = new Regex(@"\[(\d+)\]");
                Match m = bracketRegex.Match(cleanString);
                if (m.Success)
                {
                    results = bracketRegex.Replace(cleanString, $"({m.Groups[1].Value})", 1);
                }
            }

            return results;
        }
    }
}
