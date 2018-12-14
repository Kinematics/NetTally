using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NetTally.VoteCounting.RankVoteCounting.Utility;

namespace NetTally.VoteCounting.RankVoteCounting
{
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;

    class DistanceRankVoteCounter : BaseRankVoteCounter
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


            Debug.WriteLine(">>Distance Scoring<<");

            List<string> listOfChoices = GroupRankVotes.GetAllChoices(task);

            var voterRankings = GroupRankVotes.GroupByVoterAndRank(task);

            DistanceData pairwiseData = GetPairwiseData(voterRankings, listOfChoices);

            DistanceData strengthData = GetStrongestPaths(pairwiseData, listOfChoices.Count);

            DistanceData winningPaths = GetWinningPaths(strengthData, listOfChoices.Count);

            RankResults winningChoices = GetResultsInOrder(winningPaths, listOfChoices);

            return winningChoices;
        }

        #region Distance Algorithm (based on Schulze+Range)
        /// <summary>
        /// Fills the pairwise preferences.
        /// This goes through each voter's ranking options and updates an array indicating
        /// which options are preferred over which other options.
        /// Each higher-ranked option gains the difference in ranking in 'beating' a lower-ranked option.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.</param>
        /// <param name="listOfChoices">The list of choices.</param>
        /// <returns>Returns a filled-in preferences array.</returns>
        private static DistanceData GetPairwiseData(IEnumerable<VoterRankings> voterRankings, List<string> listOfChoices)
        {
            DistanceData data = new DistanceData(listOfChoices.Count, listOfChoices.Count);

            var choiceIndexes = GroupRankVotes.GetChoicesIndexes(listOfChoices);

            foreach (var voter in voterRankings)
            {
                var rankedChoices = voter.RankedVotes.Select(v => v.Vote);
                var unrankedChoices = listOfChoices.Except(rankedChoices);

                foreach (var choice in voter.RankedVotes)
                {
                    foreach (var otherChoice in voter.RankedVotes)
                    {
                        // Each ranked vote that has a higher rank (lower number) than each
                        // alternative has the distance between the choices added to the
                        // corresponding table entry.
                        if (choice.Vote != otherChoice.Vote && choice.Rank < otherChoice.Rank)
                        {
                            data.Paths[choiceIndexes[choice.Vote], choiceIndexes[otherChoice.Vote]] += otherChoice.Rank - choice.Rank;
                        }
                    }

                    // All unranked options are considered to be at distance 1 from *all* ranked options.
                    // There is no relative preference, nor does it place unranked options 'beneath'
                    // ranked options, such that higher ranked options have greater distance from them.
                    // Unranked options are agnostic choices.
                    foreach (var nonChoice in unrankedChoices)
                    {
                        data.Paths[choiceIndexes[choice.Vote], choiceIndexes[nonChoice]]++;
                    }
                }

                // All unranked options are at distance 0 from each other, and thus have no effect
                // on the distance table.
            }

            return data;
        }

        /// <summary>
        /// Calculate the strongest preference paths from the pairwise preferences table.
        /// </summary>
        /// <param name="pairwiseData">The pairwise data.</param>
        /// <param name="choicesCount">The choices count (size of the table).</param>
        /// <returns>Returns a table with the strongest paths between each pairwise choice.</returns>
        private static DistanceData GetStrongestPaths(DistanceData pairwiseData, int choicesCount)
        {
            DistanceData data = new DistanceData(choicesCount, choicesCount);

            int bytesInArray = data.Paths.Length * sizeof(Int32);
            Buffer.BlockCopy(pairwiseData.Paths, 0, data.Paths, 0, bytesInArray);

            for (int i = 0; i < choicesCount; i++)
            {
                for (int j = 0; j < choicesCount; j++)
                {
                    if (i != j)
                    {
                        for (int k = 0; k < choicesCount; k++)
                        {
                            if (i != k && j != k)
                            {
                                data.Paths[j, k] = Math.Max(data.Paths[j, k], Math.Min(data.Paths[j, i], data.Paths[i, k]));
                            }
                        }
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Gets the winning paths - The strongest of the strongest paths, for each pair option.
        /// </summary>
        /// <param name="strengthData">The strongest paths.</param>
        /// <param name="choicesCount">The choices count (size of table).</param>
        /// <returns>Returns a table with the winning choices of the strongest paths.</returns>
        private static DistanceData GetWinningPaths(DistanceData strengthData, int choicesCount)
        {
            DistanceData winningData = new DistanceData(choicesCount, choicesCount);

            for (int i = 0; i < choicesCount; i++)
            {
                for (int j = 0; j < choicesCount; j++)
                {
                    if (i != j)
                    {
                        winningData.Paths[i, j] = strengthData.Paths[i, j] - strengthData.Paths[j, i];
                    }
                }
            }

            return winningData;
        }

        /// <summary>
        /// Gets the winning options in order of preference, based on the winning paths.
        /// </summary>
        /// <param name="winningPaths">The winning paths.</param>
        /// <param name="listOfChoices">The list of choices.</param>
        /// <returns>Returns a list of </returns>
        private RankResults GetResultsInOrder(DistanceData winningPaths, List<string> listOfChoices)
        {
            int count = listOfChoices.Count;

            var availableIndexes = Enumerable.Range(0, count);

            var pathCounts = from index in availableIndexes
                             select new
                             {
                                 Index = index,
                                 Choice = listOfChoices[index],
                                 Count = GetPositivePathCount(winningPaths.Paths, index, count),
                                 Sum = GetPathSum(winningPaths.Paths, index, count),
                             };

            var orderPaths = pathCounts.OrderByDescending(p => p.Sum).ThenByDescending(p => p.Count).ThenBy(p => p.Choice);

            RankResults results = new RankResults();

            results.AddRange(orderPaths.Select(path =>
                new RankResult(listOfChoices[path.Index], $"Distance: [{path.Count}/{path.Sum}]")));

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
        private static int GetPositivePathCount(int[,] paths, int row, int count)
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
        private static int GetPathSum(int[,] paths, int row, int count)
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
