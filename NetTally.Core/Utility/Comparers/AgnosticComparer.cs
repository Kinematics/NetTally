using System;
using System.Globalization;
using NetTally.Configure;

namespace NetTally.Utility.Comparers
{
    /// <summary>
    /// Class that provides access to variable string comparers depending on
    /// quest preferences.
    /// </summary>
    public class Agnostic
    {
        public Agnostic(QuestsInfo info, IHash hash)
        {
            questsInfo = info;
            InitDependencies(hash);
        }

        /// <summary>
        /// Enum for the different combinations of comparison patterns.
        /// </summary>
        private enum AgnosticPattern
        {
            Unknown,
            NoCaseSymbol,
            NoCaseNoSymbol,
            CaseSymbol,
            CaseNoSymbol
        }

        static AgnosticStringComparer stringComparerNoCaseSymbol = null!;

        static AgnosticStringComparer stringComparerNoCaseNoSymbol = null!;

        static AgnosticStringComparer stringComparerCaseSymbol = null!;

        static AgnosticStringComparer stringComparerCaseNoSymbol = null!;

        static QuestsInfo questsInfo = null!;

        /// <summary>
        /// Get the current string comparer for the currently selected quest.
        /// </summary>
        public static AgnosticStringComparer CurrentStringComparer
        {
            get
            {
                var agnosticPattern = GetCurrentQuestComparisonPattern(questsInfo.SelectedQuest);

                return agnosticPattern switch
                {
                    AgnosticPattern.Unknown => stringComparerNoCaseNoSymbol,
                    AgnosticPattern.NoCaseSymbol => stringComparerNoCaseSymbol,
                    AgnosticPattern.NoCaseNoSymbol => stringComparerNoCaseNoSymbol,
                    AgnosticPattern.CaseSymbol => stringComparerCaseSymbol,
                    AgnosticPattern.CaseNoSymbol => stringComparerCaseNoSymbol,
                    _ => throw new NotImplementedException()
                };
            }
        }

        /// <summary>
        /// Get a case-insensitive string comparer.
        /// </summary>
        public static AgnosticStringComparer CaseInsensitiveComparer => stringComparerNoCaseSymbol;

        /// <summary>
        /// Get a string comparer that ignores both case and symbols.
        /// </summary>
        public static AgnosticStringComparer InsensitiveComparer => stringComparerNoCaseNoSymbol;

        /// <summary>
        /// Get the agnostic pattern for the given quest, based on
        /// settings for case and whitespace significance.
        /// </summary>
        /// <param name="quest"></param>
        /// <returns>An <see cref="AgnosticPattern"/> that matches the quest provided.</returns>
        private static AgnosticPattern GetCurrentQuestComparisonPattern(Quest? quest)
        {
            if (quest == null)
            {
                return AgnosticPattern.Unknown;
            }

            return quest.CaseIsSignificant switch
            {
                true => quest.WhitespaceAndPunctuationIsSignificant switch
                {
                    true => AgnosticPattern.CaseSymbol,
                    false => AgnosticPattern.CaseNoSymbol
                },
                false => quest.WhitespaceAndPunctuationIsSignificant switch
                {
                    true => AgnosticPattern.NoCaseSymbol,
                    false => AgnosticPattern.NoCaseNoSymbol
                }
            };
        }

        /// <summary>
        /// Initialize string comparers and dependency injection information.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        private static void InitDependencies(IHash hashFunction)
        {
            stringComparerNoCaseSymbol = new AgnosticStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth,
                hashFunction.HashFunction);

            // Case insensitive, whitespace/symbol insensitive
            stringComparerNoCaseNoSymbol = new AgnosticStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth,
                hashFunction.HashFunction);

            // Case sensitive, whitespace/symbol sensitive.
            stringComparerCaseSymbol = new AgnosticStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth,
                hashFunction.HashFunction);

            // Case sensitive, whitespace/symbol insensitive.
            stringComparerCaseNoSymbol = new AgnosticStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth,
                hashFunction.HashFunction);
        }
    }
}
