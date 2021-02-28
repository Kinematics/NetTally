using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTally.Forums;
using NetTally.Votes;
using NetTally.Types.Enums;

namespace NetTally.VoteCounting.RankVotes.Reference
{
    using VoteStorageEntry = KeyValuePair<VoteLineBlock, VoterStorage>;

    public class Distance : IRankVoteCounter2
    {
        public List<((int rank, double rankScore) ranking, VoteStorageEntry vote)>
            CountVotesForTask(VoteStorage taskVotes)
        {
            var listOfChoices = taskVotes.Select(v => v.Key).ToList();

            // Invert the votes so that we can look at preferences per user.
            var voterPreferences = taskVotes
                .SelectMany(v => v.Value)
                .GroupBy(u => u.Key)
                .ToDictionary(t => t.Key, s => s.Select(q => q.Value).OrderBy(r => r.MarkerValue).ToList());


            int[,] pairwiseData = GetPairwiseData(voterPreferences, listOfChoices);

            int[,] strengthData = GetStrongestPaths(pairwiseData, listOfChoices.Count);

            int[,] winningPaths = GetWinningPaths(strengthData, listOfChoices.Count);

            List<((int rank, double rankScore) ranking, VoteStorageEntry vote)> winningChoices =
                GetResultsInOrder(winningPaths, listOfChoices, taskVotes);

            return winningChoices;
        }


        #region Distance Algorithm (based on Schulze+Range)
        /// <summary>
        /// Fills the pairwise preferences.
        /// This goes through each voter's ranking options and updates an array indicating
        /// which options are preferred over which other options.
        /// Each higher-ranked option gains the difference in ranking in 'beating' a lower-ranked option.
        /// </summary>
        /// <param name="voterRankings">The voter's votes in .</param>
        /// <param name="listOfChoices">The list of choices.</param>
        /// <returns>Returns a filled-in preferences array.</returns>
        private int[,] GetPairwiseData(Dictionary<Origin, List<VoteLineBlock>> voterRankings, List<VoteLineBlock> listOfChoices)
        {
            int[,] data = new int[listOfChoices.Count, listOfChoices.Count];

            Dictionary<VoteLineBlock, int> choiceIndexes = GetChoicesIndexes(listOfChoices);

            foreach (var voter in voterRankings)
            {
                IEnumerable<VoteLineBlock> rankedChoices = voter.Value.Where(v => v.MarkerType == MarkerType.Rank);
                IEnumerable<VoteLineBlock> unrankedChoices = listOfChoices.Except(rankedChoices);

                foreach (var choice in rankedChoices)
                {
                    foreach (var otherChoice in rankedChoices)
                    {
                        // Each ranked vote that has a higher rank (lower number) than each
                        // alternative has the distance between the choices added to the
                        // corresponding table entry.
                        if ((choice != otherChoice) && (choice.MarkerValue <= otherChoice.MarkerValue))
                        {
                            data[choiceIndexes[choice], choiceIndexes[otherChoice]] += 
                                otherChoice.MarkerValue - choice.MarkerValue;
                        }
                    }

                    // All unranked options are considered to be at distance 1 from *all* ranked options.
                    // There is no relative preference, nor does it place unranked options 'beneath'
                    // ranked options, such that higher ranked options have greater distance from them.
                    // Unranked options are agnostic choices.
                    foreach (var nonChoice in unrankedChoices)
                    {
                        data[choiceIndexes[choice], choiceIndexes[nonChoice]]++;
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
        private int[,] GetStrongestPaths(int[,] pairwiseData, int choicesCount)
        {
            int[,] data = new int[choicesCount, choicesCount];

            // Copy the original data to a new array that we'll be working on.
            int bytesInArray = data.Length * sizeof(Int32);
            Buffer.BlockCopy(pairwiseData, 0, data, 0, bytesInArray);

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
                                data[j, k] = Math.Max(data[j, k], Math.Min(data[j, i], data[i, k]));
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
        private int[,] GetWinningPaths(int[,] strengthData, int choicesCount)
        {
            int[,] winningData = new int[choicesCount, choicesCount];

            for (int i = 0; i < choicesCount; i++)
            {
                for (int j = 0; j < choicesCount; j++)
                {
                    if (i != j)
                    {
                        winningData[i, j] = strengthData[i, j] - strengthData[j, i];
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
        private List<((int rank, double rankScore) ranking, VoteStorageEntry vote)>
            GetResultsInOrder(int[,] winningPaths, List<VoteLineBlock> listOfChoices, VoteStorage taskVotes)
        {
            var availableIndexes = Enumerable.Range(0, listOfChoices.Count);

            var pathCounts = from index in availableIndexes
                             select new
                             {
                                 Index = index,
                                 Choice = listOfChoices[index],
                                 Count = GetPositivePathCount(winningPaths, index, listOfChoices.Count),
                                 Sum = GetPathSum(winningPaths, index, listOfChoices.Count)
                             };

            var orderPaths = pathCounts.OrderByDescending(p => p.Count)
                                       .ThenByDescending(p => p.Sum)
                                       .ThenBy(p => p.Choice)
                                       .ToList();

            List<((int rank, double rankScore) ranking, VoteStorageEntry vote)> results
                = new List<((int rank, double rankScore) ranking, VoteStorageEntry vote)>();

            for (int i = 0; i < orderPaths.Count; i++)
            {
                var entry = ((i + 1, (double)orderPaths[i].Count / orderPaths[i].Sum),
                    new VoteStorageEntry(orderPaths[i].Choice, taskVotes[orderPaths[i].Choice]));
                results.Add(entry);
            }

            return results;
        }
        #endregion

        #region Small Utility
        /// <summary>
        /// Convert a list to a lookup of values to index.
        /// Assumes that all the entries in the incoming list are unique.
        /// </summary>
        /// <param name="list">The list to convert.</param>
        /// <returns>Returns a dictionary pairing each list entry with its index.</returns>
        private Dictionary<T, int> GetChoicesIndexes<T>(List<T> list)
        {
            Dictionary<T, int> indexes = new Dictionary<T, int>();

            for (int i = 0; i < list.Count; i++)
            {
                indexes.Add(list[i], i);
            }

            return indexes;
        }


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
