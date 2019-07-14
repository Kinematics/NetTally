using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NetTally.Utility;
using NetTally.Extensions;
using NetTally.Votes;

namespace NetTally.Output
{
    /// <summary>
    /// Meta-vote class to allow storing the primary vote, along with any
    /// 'child' votes that have the same first line of text as the main vote.
    /// </summary>
    public class VoteNode
    {
        readonly VoteNode? Parent;
        readonly VoteInfo voteInfo;

        public string Text { get; set; }

        public List<VoteNode> Children { get; } = new List<VoteNode>();
        public HashSet<string> Voters { get; } = new HashSet<string>();
        public HashSet<string> AllVoters { get; } = new HashSet<string>();

        public int VoterCount => AllVoters.Count(v => !v.IsPlanName());

        bool HasParent => Parent != null;
        bool HasChildren => Children.Count > 0;

        public VoteNode(string text, HashSet<string>? voters, VoteInfo info)
            : this(text, voters, info, null)
        {
        }

        private VoteNode(string text, HashSet<string>? voters, VoteInfo info, VoteNode? parent)
        {
            Parent = parent;
            Text = text;
            voteInfo = info;
            AddVoters(voters);
        }

        /// <summary>
        /// Add new voters to this node's voter list.
        /// </summary>
        /// <param name="voters">Voters to add.</param>
        public void AddVoters(HashSet<string>? voters)
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
            VoteNode child = Children.FirstOrDefault(c => Agnostic.StringComparer.Equals(c.Text, text));

            if (child == null)
            {
                child = new VoteNode(text, voters, voteInfo, this);
                Children.Add(child);
            }
            else
            {
                child.AddVoters(voters);
            }

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
            var lines = Text.GetStringLines();
            if (lines.Count == 0)
                return string.Empty;

            string results = string.Empty;

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
                    if (!string.IsNullOrEmpty(task))
                        sb.Append($"[{task}]");
                }


                sb.Append(" Plan: ");
                string? firstVoter = voteInfo.GetFirstVoter(Voters);
                sb.Append(firstVoter);

                // Only add the link if we're not showing the voters
                if (displayMode == DisplayMode.CompactNoVoters && firstVoter != null)
                {
                    string link;

                    VoteType voteType = firstVoter.IsPlanName() ? VoteType.Plan : VoteType.Vote;
                    link = voteInfo.GetVoterUrl(firstVoter, voteType);

                    sb.Append(" — ");
                    sb.Append(link);
                }

                results = sb.ToString();
            }

            // Child nodes in compact mode will be put in spoilers.  Remove BBCode.
            if (HasParent && displayMode == DisplayMode.Compact)
            {
                results = VoteString.RemoveBBCode(results);
            }

            return results;
        }
    }
}
