using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally
{
    public interface IQuest
    {
        /// <summary>
        /// The web site that the quest is on.
        /// </summary>
        string Site { get; set; }
        /// <summary>
        /// The name of the quest (ie: the name of the thread in the URL).
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// Starting post to start tallying from.
        /// </summary>
        int StartPost { get; set; }
        /// <summary>
        /// Ending post for the tally to run to.
        /// </summary>
        int EndPost { get; set; }

        /// <summary>
        /// Flag for whether to try to override the provided starting post by
        /// looking for the last threadmark.
        /// </summary>
        bool CheckForLastThreadmark { get; set; }
        /// <summary>
        /// Flag for whether to use vote partitions for this quest.
        /// </summary>
        bool UseVotePartitions { get; set; }
        /// <summary>
        /// Flag for whether vote partitions should be done by line or by block.
        /// </summary>
        bool PartitionByLine { get; set; }

        /// <summary>
        /// Derived property.
        /// </summary>
        bool ReadToEndOfThread { get; }

        /// <summary>
        /// Get the forum adapter needed to read results from the web site this
        /// quest is for.
        /// </summary>
        /// <returns></returns>
        IForumAdapter GetForumAdapter();
    }
}
