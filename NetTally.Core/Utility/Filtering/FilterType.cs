using System;
using System.Collections.Generic;
using System.Text;

namespace NetTally.Utility.Filtering
{
    public enum FilterType
    {
        /// <summary>
        /// Default value. Ignore all filtering mechanics.
        /// </summary>
        Unset,
        /// <summary>
        /// Have the filter set up to block things.
        /// </summary>
        Block,
        /// <summary>
        /// Have the filter set up to allow things.
        /// </summary>
        Allow,
    }

}
