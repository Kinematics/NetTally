using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTally.Forums;
using NetTally.Votes;

namespace NetTally.VoteCounting.RankVotes.Reference
{
    using VoteStorageEntry = KeyValuePair<VoteLineBlock, VoterStorage>;

    public class Pairwise : IRankVoteCounter2
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

            int[,] pairwisePreferences = GetPairwisePreferences(voterPreferences, listOfChoices);

            int[,] pairwiseWinners = GetPairwiseWinners(pairwisePreferences, listOfChoices.Count);

            List<((int rank, double rankScore) ranking, VoteStorageEntry vote)> winningChoices =
                GetResultsInOrder(pairwiseWinners, listOfChoices, taskVotes);

            return winningChoices;
        }

        #region Math
        /// <summary>
        /// Fills the pairwise preferences.
        /// This goes through each voter's ranking options and updates an array indicating
        /// which options are preferred over which other options.  Each higher-ranked
        /// option gains one point in 'beating' a lower-ranked option.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.</param>
        /// <param name="listOfChoices">The list of choices.</param>
        /// <returns>Returns a filled-in preferences array.</returns>
        private int[,] GetPairwisePreferences(Dictionary<Origin, List<VoteLineBlock>> voterRankings, List<VoteLineBlock> listOfChoices)
        {
            int[,] pairwisePreferences = new int[listOfChoices.Count, listOfChoices.Count];

            Dictionary<VoteLineBlock, int> choiceIndexes = GetChoicesIndexes(listOfChoices);

            foreach (var voter in voterRankings)
            {
                IEnumerable<VoteLineBlock> rankedChoices = voter.Value.Where(v => v.MarkerType == MarkerType.Rank);
                IEnumerable<VoteLineBlock> unrankedChoices = listOfChoices.Except(rankedChoices);

                foreach (var choice in rankedChoices)
                {
                    // Each choice matching or beating the ranks of other ranked choices is marked.
                    foreach (var otherChoice in rankedChoices)
                    {
                        if ((choice != otherChoice) && (choice.MarkerValue <= otherChoice.MarkerValue))
                        {
                            pairwisePreferences[choiceIndexes[choice], choiceIndexes[otherChoice]]++;
                        }
                    }

                    // Each choice is ranked higher than all unranked choices
                    foreach (var nonChoice in unrankedChoices)
                    {
                        pairwisePreferences[choiceIndexes[choice], choiceIndexes[nonChoice]]++;
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
