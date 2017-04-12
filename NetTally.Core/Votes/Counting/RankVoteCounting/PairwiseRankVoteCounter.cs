using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NetTally.VoteCounting.RankVoteCounting.Utility;

namespace NetTally.VoteCounting.RankVoteCounting
{
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;

    public class PairwiseRankVoteCounter : BaseRankVoteCounter
    {
        /// <summary>
        /// Implementation to generate the ranking list for the provided set
        /// of votes for a specific task, based on the Schulze algorithm.
        /// </summary>
        /// <param name="task">The task that the votes are grouped under.</param>
        /// <returns>Returns a ranking list of winning votes.</returns>
        protected override RankResults RankTask(GroupedVotesByTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));


            Debug.WriteLine(">>Pairwise Ranking<<");

            List<string> listOfChoices = GroupRankVotes.GetAllChoices(task);

            var voterRankings = GroupRankVotes.GroupByVoterAndRank(task);

            int[,] pairwisePreferences = GetPairwisePreferences(voterRankings, listOfChoices);

            int[,] pairwiseWinners = GetPairwiseWinners(pairwisePreferences, listOfChoices.Count);

            RankResults winningChoices = GetResultsInOrder(pairwiseWinners, listOfChoices);

            return winningChoices;
        }

        #region Schulze Algorithm
        /// <summary>
        /// Fills the pairwise preferences.
        /// This goes through each voter's ranking options and updates an array indicating
        /// which options are preferred over which other options.  Each higher-ranked
        /// option gains one point in 'beating' a lower-ranked option.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.</param>
        /// <param name="listOfChoices">The list of choices.</param>
        /// <returns>Returns a filled-in preferences array.</returns>
        private int[,] GetPairwisePreferences(IEnumerable<VoterRankings> voterRankings, List<string> listOfChoices)
        {
            int[,] pairwisePreferences = new int[listOfChoices.Count, listOfChoices.Count];

            var choiceIndexes = GroupRankVotes.GetChoicesIndexes(listOfChoices);

            foreach (var voter in voterRankings)
            {
                var rankedChoices = voter.RankedVotes.Select(v => v.Vote);
                var unrankedChoices = listOfChoices.Except(rankedChoices);

                foreach (var choice in voter.RankedVotes)
                {
                    // Each choice matching or beating the ranks of other ranked choices is marked.
                    foreach (var otherChoice in voter.RankedVotes)
                    {
                        if ((choice.Vote != otherChoice.Vote) && (choice.Rank <= otherChoice.Rank))
                        {
                            pairwisePreferences[choiceIndexes[choice.Vote], choiceIndexes[otherChoice.Vote]]++;
                        }
                    }

                    // Each choice is ranked higher than all unranked choices
                    foreach (var nonChoice in unrankedChoices)
                    {
                        pairwisePreferences[choiceIndexes[choice.Vote], choiceIndexes[nonChoice]]++;
                    }
                }
            }

            return pairwisePreferences;
        }

        /// <summary>
        /// Gets the winning paths - The strongest of the strongest paths, for each pair option.
        /// </summary>
        /// <param name="pairwisePreferences">The strongest paths.</param>
        /// <param name="choicesCount">The choices count (size of table).</param>
        /// <returns>Returns a table with the winning choices of the strongest paths.</returns>
        private int[,] GetPairwiseWinners(int[,] pairwisePreferences, int choicesCount)
        {
            int[,] winningPaths = new int[choicesCount, choicesCount];

            for (int i = 0; i < choicesCount; i++)
            {
                for (int j = 0; j < choicesCount; j++)
                {
                    if (i != j)
                    {
                        if (pairwisePreferences[i, j] >= pairwisePreferences[j, i])
                        {
                            winningPaths[i, j] = pairwisePreferences[i, j];
                        }
                        else
                        {
                            winningPaths[i, j] = 0;
                        }
                    }
                }
            }

            return winningPaths;
        }

        /// <summary>
        /// Gets the winning options in order of preference, based on the winning paths.
        /// </summary>
        /// <param name="winningPaths">The winning paths.</param>
        /// <param name="listOfChoices">The list of choices.</param>
        /// <returns>Returns a list of </returns>
        private RankResults GetResultsInOrder(int[,] winningPaths, List<string> listOfChoices)
        {
            int count = listOfChoices.Count;

            var availableIndexes = Enumerable.Range(0, count);

            var pathCounts = from index in availableIndexes
                             select new
                             {
                                 Index = index,
                                 Choice = listOfChoices[index],
                                 Count = GetPositivePathCount(winningPaths, index, count),
                                 Sum = GetPathSum(winningPaths, index, count)
                             };

            var orderPaths = pathCounts.OrderByDescending(p => p.Count).ThenByDescending(p => p.Sum).ThenBy(p => p.Choice);

            RankResults results = new RankResults();

            results.AddRange(orderPaths.Select(path =>
                new RankResult(listOfChoices[path.Index], $"Pairwise: [{path.Count}/{path.Sum}]")));

            return results;
        }
        #endregion

        #region Small Utility        
        /// <summary>
        /// Gets the number of paths in the table with a value greater than 0.
        /// </summary>
        /// <param name="paths">The paths table.</param>
        /// <param name="row">The row.</param>
        /// <param name="count">The size of the table.</param>
        /// <returns>Returns a count of the number of positive path strength values.</returns>
        private int GetPositivePathCount(int[,] paths, int row, int count)
        {
            int pathCount = 0;

            for (int i = 0; i < count; i++)
            {
                if (paths[row, i] > 0)
                    pathCount++;
            }

            return pathCount;
        }

        /// <summary>
        /// Gets the sum of the path strength for a given table row.
        /// </summary>
        /// <param name="paths">The paths table.</param>
        /// <param name="row">The row.</param>
        /// <param name="count">The size of the table.</param>
        /// <returns>Returns the sum of the given path.</returns>
        private int GetPathSum(int[,] paths, int row, int count)
        {
            int pathSum = 0;

            for (int i = 0; i < count; i++)
            {
                pathSum += paths[row, i];
            }

            return pathSum;
        }
        #endregion

    }
}
