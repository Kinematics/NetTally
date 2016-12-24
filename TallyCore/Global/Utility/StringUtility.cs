using System;
using System.Collections.Generic;
using System.Globalization;

namespace NetTally.Utility
{
    /// <summary>
    /// Class for general static functions relating to text manipulation and comparisons.
    /// </summary>
    public static class StringUtility
    {
        /// <summary>
        /// Magic character (currently ◈, \u25C8) to mark a named voter as a plan rather than a user.
        /// </summary>
        public static string PlanNameMarker { get; } = "◈";

        /// <summary>
        /// Check if the provided name starts with the plan name marker.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>Returns true if the name starts with the plan name marker.</returns>
        public static bool IsPlanName(this string name) => name?.StartsWith(PlanNameMarker, StringComparison.Ordinal) ?? false;

    }
}
