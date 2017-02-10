using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NetTally.Votes.Experiment
{
    /// <summary>
    /// Class to store and reason about vote plans.
    /// </summary>
    public class Plan
    {
        #region Properties
        public Identity Identity { get; }
        public PlanType PlanType { get; }
        public VotePartition Content { get; }

        private List<VotePartition> partitionedContent = new List<VotePartition>();
        public IReadOnlyList<VotePartition> PartitionedContent { get { return partitionedContent; } }
        #endregion

        #region Constructor        
        /// <summary>
        /// Initializes a new instance of the <see cref="Plan"/> class.
        /// </summary>
        /// <param name="planName">Name of the plan.</param>
        /// <param name="sourceIdentity">The identity of the originating post.</param>
        /// <param name="content">The content (<see cref="VotePartition"/>) of the plan.</param>
        /// <param name="planType">Type of the plan.</param>
        public Plan(string planName, Identity sourceIdentity, VotePartition content, PlanType planType)
        {
            if (string.IsNullOrEmpty(planName))
                throw new ArgumentNullException(nameof(planName));

            Identity = new Identity(planName, sourceIdentity, IdentityType.Plan);
            Content = content ?? throw new ArgumentNullException(nameof(content));
            PlanType = planType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plan" /> class.
        /// </summary>
        /// <param name="identity">The identity object for the plan.</param>
        /// <param name="content">The content (<see cref="VotePartition"/>) of the plan.</param>
        /// <param name="planType">Type of the plan.</param>
        public Plan(Identity identity, VotePartition content, PlanType planType)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            PlanType = planType;
        }
        #endregion

        #region Public methods        
        /// <summary>
        /// Sets the partitions that the content was broken into, based on partition mode settings.
        /// </summary>
        /// <param name="partitions">The partitions to store.</param>
        public void SetContentPartitions(IEnumerable<VotePartition> partitions)
        {
            partitionedContent.Clear();
            partitionedContent.AddRange(partitions);
        }
        #endregion

        #region Equality comparisons
        public override bool Equals(object obj)
        {
            if (obj is Plan otherPlan)
            {
                return Identity == otherPlan.Identity && Content == otherPlan.Content;
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
                    var partition = new VotePartition(voteGroup, VoteType.Plan);
                    plans.Add(new Plan(m.Groups["planname"].Value, vote.Post.Identity, partition, PlanType.Base));
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

                    var flattenedLines = labeledGroups.SelectMany(a => a).ToList();
                    var partition = new VotePartition(flattenedLines, VoteType.Plan);

                    PlanType planType = labeledGroups.Skip(1).Any() ? PlanType.Label : PlanType.SingleLine;

                    voteGrouping = voteGrouping.Skip(labeledGroups.Count());

                    plans.Add(new Plan(m.Groups["planname"].Value, vote.Post.Identity, partition, planType));
                }
            }

            // Any other defined plans with content
            foreach (var voteGroup in voteGrouping)
            {
                Match m = anyPlanRegex.Match(voteGroup.Key);
                if (m.Success && voteGroup.Skip(1).Any())
                {
                    var partition = new VotePartition(voteGroup, VoteType.Plan);
                    plans.Add(new Plan(m.Groups["planname"].Value, vote.Post.Identity, partition, PlanType.Content));
                }
            }

            vote.StorePlans(plans);

            // Return all collected plans
            return plans;
        }
        #endregion

    }
}
