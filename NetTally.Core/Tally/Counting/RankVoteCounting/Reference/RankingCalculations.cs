﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTally.Votes;
using NetTally.Types.Enums;

namespace NetTally.VoteCounting.RankVotes.Reference
{
    using VoteStorageEntry = KeyValuePair<VoteLineBlock, VoterStorage>;

    public static class RankingCalculations
    {
        /// <summary>
        /// Calculates the Borda score for the provided list of voter rankings of a given vote.
        /// </summary>
        /// <param name="votes">Votes with rank information included.</param>
        /// <returns>Returns a numeric evaluation of the overall score of the vote.</returns>
        public static double BordaScore(VoteStorageEntry votes)
        {
            double voteValue = 0;

            // Normalize to 9 points for #1, 8 points for #2, etc.
            foreach (var vote in votes.Value)
            {
                if (vote.Value.MarkerType == MarkerType.Rank && vote.Value.MarkerValue > 0 && vote.Value.MarkerValue < 10)
                {
                    voteValue += (10 - vote.Value.MarkerValue);
                }
            }

            return voteValue;
        }

        /// <summary>
        /// Calculates the inverse Borda score for the provided list of voter rankings of a given vote.
        /// </summary>
        /// <param name="votes">Votes with rank information included.</param>
        /// <returns>Returns a numeric evaluation of the overall score of the vote.</returns>
        public static double InverseBordaScore(VoteStorageEntry votes)
        {
            double voteValue = 0;

            // Value of each rank is 1/N.
            foreach (var vote in votes.Value)
            {
                if (vote.Value.MarkerType == MarkerType.Rank && vote.Value.MarkerValue > 0 && vote.Value.MarkerValue < 10)
                {
                    voteValue += (1.0 / vote.Value.MarkerValue);
                }
            }

            return voteValue;
        }


        /// <summary>
        /// Calculates the lower bound of the Wilson score for the provided list of voter rankings of a given vote.
        /// Reference: http://www.evanmiller.org/how-not-to-sort-by-average-rating.html
        /// </summary>
        /// <param name="votes">Votes with associated ranks, for the voters who ranked the vote with a given value.</param>
        /// <returns>Returns a numeric evaluation of the overall score of the vote.</returns>
        public static (double score, int count) LowerWilsonRankingScore(VoteStorageEntry votes)
        {
            int n = votes.Value.Count(v => v.Value.MarkerType == MarkerType.Rank);

            if (n == 0)
                return (0, 0);

            double positiveScore = 0.0;
            double negativeScore = 0.0;

            // Add up the sum of the number of voters times the value of each rank.
            // Value of each rank is 1/N.
            foreach (var vote in votes.Value)
            {
                if (vote.Value.MarkerType == MarkerType.Rank && vote.Value.MarkerValue > 0 && vote.Value.MarkerValue < 10)
                {
                    double scaledPositiveScore = PositivePortionOf9RankScale(vote.Value.MarkerValue);

                    positiveScore += scaledPositiveScore;
                    negativeScore += (1.0 - scaledPositiveScore);
                }
            }

            double p̂ = positiveScore / (positiveScore + negativeScore);
            double z = 1.96;
            double sqTerm = (p̂ * (1 - p̂) + z * z / (4 * n)) / n;

            double lowerWilson = (p̂ + (z * z / (2 * n)) - z * Math.Sqrt(sqTerm)) / (1 + z * z / n);

            return (lowerWilson, n);
        }

        private static double PositivePortionOf9RankScale(int rank)
        {
            // Rank 1 = 1.0 positive, 0.0 negative
            // Rank 2 = 0.875 positive, 0.125 negative
            // Rank 3 = 0.75 positive, 0.25 negative
            // Rank 4 = 0.625 positive, 0.375 negative
            // Rank 5 = 0.5 positive, 0.5 negative
            // Rank 6 = 0.375 positive, 0.625 negative
            // Rank 7 = 0.25 positive, 0.75 negative
            // Rank 8 = 0.125 positive, 0.875 negative
            // Rank 9 = 0.0 positive, 1.0 negative

            if (rank < 1)
                return 0;

            if (rank > 9)
                rank = 9;

            int decrement = rank - 1;

            return 1.0 - (decrement * 0.125);
        }

        public static (double rating, double lowerBound) GetLowerWilsonScore<T>(
            IEnumerable<T> source, Func<T, double> transform, double scaling = 100.0)
        {
            if (!source.Any())
                return (0, 0);

            double positiveScore = 0.0;
            double negativeScore = 0.0;
            int n = 0;

            foreach (var item in source)
            {
                double itemScore = transform(item) / scaling;

                positiveScore += itemScore;
                negativeScore += (1.0 - itemScore);
                n++;
            }

            double p̂ = positiveScore / (positiveScore + negativeScore);
            double z = 1.96;
            double sqTerm = (p̂ * (1 - p̂) + z * z / (4 * n)) / n;

            double lowerWilson = (p̂ + (z * z / (2 * n)) - z * Math.Sqrt(sqTerm)) / (1 + z * z / n);

            return (p̂ * scaling, lowerWilson * scaling);
        }


    }
}
