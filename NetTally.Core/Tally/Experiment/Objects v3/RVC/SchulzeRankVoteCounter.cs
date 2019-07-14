using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTally.VoteCounting;
using NetTally.VoteCounting.RankVoteCounting.Utility;

namespace NetTally.Experiment3
{
    public class SchulzeRankVoteCounter : IRankVoteCounter2
    {
        public List<((int rank, double rankScore) ranking, KeyValuePair<VoteLineBlock, VoterStorage> vote)>
            CountVotesForTask(VoteStorage taskVotes)
        {
            int[,] pairwisePreferences = GetPairwisePreferences(taskVotes);

            int[,] strongestPaths = GetStrongestPaths(pairwisePreferences, taskVotes.Count);

            int[,] winningPaths = GetWinningPaths(strongestPaths, taskVotes.Count);

            List<((int rank, double rankScore) ranking, KeyValuePair<VoteLineBlock, VoterStorage> vote)> winningChoices = 
                GetResultsInOrder(winningPaths, taskVotes);

            return winningChoices;
        }

        private int[,] GetPairwisePreferences(VoteStorage taskVotes)
        {
            int[,] pairwisePreferences = new int[taskVotes.Count, taskVotes.Count];

            var choiceIndexes = GetChoicesIndexes(taskVotes.Keys);

            // Invert the votes so that we can look at preferences per user.
            var voterPreferences = taskVotes.SelectMany(v => v.Value).GroupBy(u => u.Key).ToDictionary(t => t.Key, s => s.Select(q => q.Value).ToHashSet());

            foreach (var voter in voterPreferences)
            {
                // We want a list of everything each voter didn't vote for.
                var unrankedChoices1 = taskVotes.Keys.Except(voter.Value);

                foreach (var choice in voter.Value)
                {
                    // Each choice matching or beating the ranks of other ranked choices is marked.
                    foreach (var otherChoice in voter.Value)
                    {
                        if ((choice != otherChoice) && (choice.MarkerValue <= otherChoice.MarkerValue))
                        {
                            pairwisePreferences[choiceIndexes[choice], choiceIndexes[otherChoice]]++;
                        }
                    }

                    // Each choice is ranked higher than all unranked choices
                    foreach (var nonChoice in unrankedChoices1)
                    {
                        pairwisePreferences[choiceIndexes[choice], choiceIndexes[nonChoice]]++;
                    }
                }
            }

            return pairwisePreferences;
        }

        /// <summary>
        /// Gets an indexer lookup for the list of choices, so it doesn't have to do
        /// sequential lookups each time..
        /// </summary>
        /// <param name="listOfChoices">The list of choices.</param>
        /// <returns>Returns a dictionary of choices vs list index.</returns>
        private Dictionary<VoteLineBlock, int> GetChoicesIndexes(IEnumerable<VoteLineBlock> listOfChoices)
        {
            Dictionary<VoteLineBlock, int> choiceIndexes = new Dictionary<VoteLineBlock, int>();

            int index = 0;
            foreach (var choice in listOfChoices)
            {
                choiceIndexes[choice] = index++;
            }

            return choiceIndexes;
        }

        /// <summary>
        /// Calculate the strongest preference paths from the pairwise preferences table.
        /// </summary>
        /// <param name="pairwisePreferences">The pairwise preferences.</param>
        /// <param name="choicesCount">The choices count (size of the table).</param>
        /// <returns>Returns a table with the strongest paths between each pairwise choice.</returns>
        private int[,] GetStrongestPaths(int[,] pairwisePreferences, int choicesCount)
        {
            int[,] strongestPaths = new int[choicesCount, choicesCount];

            int bytesInArray = strongestPaths.Length * sizeof(Int32);
            Buffer.BlockCopy(pairwisePreferences, 0, strongestPaths, 0, bytesInArray);

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
                                strongestPaths[j, k] = Math.Max(strongestPaths[j, k], Math.Min(strongestPaths[j, i], strongestPaths[i, k]));
                            }
                        }
                    }
                }
            }

            return strongestPaths;
        }

        /// <summary>
        /// Gets the winning paths - The strongest of the strongest paths, for each pair option.
        /// </summary>
        /// <param name="strongestPaths">The strongest paths.</param>
        /// <param name="choicesCount">The choices count (size of table).</param>
        /// <returns>Returns a table with the winning choices of the strongest paths.</returns>
        private int[,] GetWinningPaths(int[,] strongestPaths, int choicesCount)
        {
            int[,] winningPaths = new int[choicesCount, choicesCount];

            for (int i = 0; i < choicesCount; i++)
            {
                for (int j = 0; j < choicesCount; j++)
                {
                    if (i != j)
                    {
                        if (strongestPaths[i, j] >= strongestPaths[j, i])
                        {
                            winningPaths[i, j] = strongestPaths[i, j];
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
        private List<((int rank, double rankScore) ranking, KeyValuePair<VoteLineBlock, VoterStorage> vote)>
            GetResultsInOrder(int[,] winningPaths, Dictionary<VoteLineBlock, VoterStorage> listOfChoices)
        {
            int count = listOfChoices.Count;

            var availableIndexes = Enumerable.Range(0, count);

            var pathCounts = from index in availableIndexes
                             select new
                             {
                                 Index = index,
                                 Choice = listOfChoices.ElementAt(index),
                                 Count = GetPositivePathCount(winningPaths, index, count),
                                 Sum = GetPathSum(winningPaths, index, count)
                             };

            var orderPaths = pathCounts.OrderByDescending(p => p.Count).ThenByDescending(p => p.Sum);


            int r = 1;

            List<((int rank, double rankScore) ranking, KeyValuePair<VoteLineBlock, VoterStorage> vote)> resultList
                = new List<((int rank, double rankScore) ranking, KeyValuePair<VoteLineBlock, VoterStorage> vote)>();

            foreach (var res in orderPaths)
            {
                resultList.Add(((r++, res.Sum), res.Choice));
            }

            return resultList;
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

    }
}


