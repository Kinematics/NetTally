using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally
{
    public interface IQuest
    {
        string Name { get; set; }
        int StartPost { get; set; }
        int EndPost { get; set; }

        /// <summary>
        /// Flag for whether to try to override the provided starting post by
        /// looking for the last threadmark.
        /// </summary>
        bool CheckForLastThreadmark { get; set; }
        bool UseVotePartitions { get; set; }
        bool PartitionByLine { get; set; }

        bool ReadToEndOfThread { get; }
    }
}
