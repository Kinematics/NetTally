using System;
using System.Collections.Generic;
using System.Text;

namespace NetTally.Utility.Filtering
{
    public enum ListFilterType
    {
        /// <summary>
        /// Ignore the filter list, and do not filter anything.
        /// </summary>
        Ignore,
        /// <summary>
        /// Exclude anything found in the filter list.
        /// </summary>
        Exclude,
        /// <summary>
        /// Only allow what is in the filter list.
        /// </summary>
        Include,
    }

}
