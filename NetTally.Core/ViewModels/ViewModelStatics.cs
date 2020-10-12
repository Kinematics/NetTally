using System.Collections.Generic;
using System.Linq;
using NetTally.Extensions;

namespace NetTally.ViewModels
{
    /// <summary>
    /// A collection of static properties that idealy would be added to the existing ViewModel class.
    /// </summary>
    public partial class ViewModelStatics
    {
        /// <summary>
        /// Number of ValidPostsPerPage
        /// </summary>
        public static readonly List<int> ValidPostsPerPage = new List<int> { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };


        /// <summary>
        /// Rank Vote counting modes, defined from the <see cref="VoteCounting.RankVoteCounterMethod"/> <see langword="enum"/>.
        /// </summary>
        public static readonly List<string> RankVoteCountingModes = EnumExtensions.EnumDescriptionsList<VoteCounting.RankVoteCounterMethod>().ToList();
    }
}
