using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NetTally.Extensions;
using NetTally.Utility;

namespace NetTally.Votes.Experiment
{
    /// <summary>
    /// Class to store and reason about vote plans.
    /// </summary>
    public class Plan
    {
        #region Constructor
        public Plan(PlanType planType, string name, Identity identity, List<VoteLine> lines)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            Lines = lines ?? throw new ArgumentNullException(nameof(lines));

            PlanType = planType;
            Identity = new Identity(name, identity, IdentityType.Plan);
        }
        #endregion

        #region Properties
        public Identity Identity { get; }
        public PlanType PlanType { get; }
        public List<VoteLine> Lines { get; }
        #endregion

        #region Static class usage
        // Check for a plan reference. "Plan: Dwarf Raid"
        static readonly Regex anyPlanRegex = new Regex(@"^(?<base>base\s*)?plan(:|\s)+◈?(?<planname>.+)\.?$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets the plans from a vote.
        /// </summary>
        /// <param name="vote">The vote.</param>
        /// <returns>Returns a list of all plans found within a vote.</returns>
        public static List<Plan> GetPlansFromVote(Vote vote)
        {
            if (vote == null)
                throw new ArgumentNullException(nameof(vote));

            if (!vote.IsValid)
                throw new InvalidOperationException("This is not a valid vote.");

            List<Plan> plans = new List<Plan>();

            var voteGrouping = vote.GetVoteMarkerGroups();

            bool checkForBasePlans = true;

            // Base plans
            while (checkForBasePlans && voteGrouping.Any())
            {
                var voteGroup = voteGrouping.First();

                Match m = anyPlanRegex.Match(voteGroup.Key);

                if (m.Groups["base"].Success && voteGroup.First().MarkerType == MarkerType.Vote && voteGroup.Count() > 1)
                {
                    plans.Add(new Plan(PlanType.Base, m.Groups["planname"].Value, vote.Post.Identity, voteGroup.ToList()));
                    voteGrouping = voteGrouping.Skip(1);
                }
                else
                {
                    checkForBasePlans = false;
                }
            }

            // Vote labels
            if (voteGrouping.Any())
            {
                var voteGroup = voteGrouping.First();

                Match m = anyPlanRegex.Match(voteGroup.Key);

                if (m.Success && m.Groups["base"].Success == false && voteGroup.Count() == 1 &&
                    voteGroup.First().MarkerType == MarkerType.Vote)
                {
                    var labeledGroups = voteGrouping.TakeWhile(g => g.First().MarkerType == MarkerType.Vote ||
                        g.First().MarkerType == MarkerType.Approval);

                    PlanType labelType = labeledGroups.Skip(1).Any() ? PlanType.Label : PlanType.SingleLine;

                    voteGrouping = voteGrouping.Skip(labeledGroups.Count());

                    plans.Add(new Plan(labelType, m.Groups["planname"].Value, vote.Post.Identity,
                        labeledGroups.SelectMany(a => a).ToList()));
                }
            }

            // Any other defined plans with content
            foreach (var voteGroup in voteGrouping)
            {
                Match m = anyPlanRegex.Match(voteGroup.Key);
                if (m.Success && voteGroup.Skip(1).Any())
                {
                    plans.Add(new Plan(PlanType.Content, m.Groups["planname"].Value, vote.Post.Identity, voteGroup.ToList()));
                }
            }

            vote.StorePlans(plans);

            // Return all collected plans
            return plans;
        }
        #endregion

        #region Equality comparisons
        public override bool Equals(object obj)
        {
            if (obj is Plan otherPlan)
            {
                if (Lines.Count != otherPlan.Lines.Count)
                    return false;

                var pairs = Lines.Zip(otherPlan.Lines, (first, second) => Agnostic.StringComparer.Equals(first.CleanContent, second.CleanContent));

                return pairs.All(p => p);
            }

            return false;
        }

        public static bool operator ==(Plan left, Plan right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }

        public static bool operator !=(Plan left, Plan right)
        {
            if (ReferenceEquals(left, null))
            {
                return !ReferenceEquals(right, null);
            }
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

    }
}
