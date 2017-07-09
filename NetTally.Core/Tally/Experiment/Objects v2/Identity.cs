using System;
using System.Collections.Generic;
using System.Text;
using NetTally.Utility;

namespace NetTally.Votes.Experiment2
{
    class Identity : IEquatable<Identity>
    {
        public string BasicName { get; }
        public string Name { get; }
        public int Number { get; }
        public bool IsPlan { get; }

        protected Identity(string name, bool isPlan, int number)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            BasicName = name;
            IsPlan = isPlan;
            Number = number;

            string variant = IsPlan ? number > 0 ? $" ({number})" : "" : "";
            string marker = IsPlan ? Strings.PlanNameMarker.ToString() : "";

            Name = $"{marker}{BasicName}{variant}";
        }

        public bool Matches(string compare)
        {
            return Agnostic.StringComparer.Equals(BasicName, compare);
        }

        public bool Equals(Identity other)
        {
            return Matches(other.BasicName) && Number == other.Number && IsPlan == other.IsPlan;
        }

        public override bool Equals(object obj)
        {
            if (obj is Identity other)
                return this.Equals(other);

            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
