using System;
using System.Collections.Generic;

namespace NetTally.Votes.Experiment
{
    /// <summary>
    /// Bucket class to hold a plan, with info about the type and name of the plan.
    /// </summary>
    public class PlanDescriptor
    {
        public string Name { get; }
        public PlanType PlanType { get; }
        public List<VoteLine> Lines { get; }

        public PlanDescriptor(PlanType planType, string name, List<VoteLine> lines)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            Name = name;
            PlanType = planType;
            Lines = lines ?? throw new ArgumentNullException(nameof(lines));
        }
    }
}
