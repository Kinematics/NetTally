using System;
using System.ComponentModel;
using System.Globalization;

namespace NetTally.Utility.Comparers
{
    public class Agnostic : IAgnostic
    {
        #region Fields and properties
        static StringComparer StringComparerNoCaseSymbol { get; set; } = StringComparer.InvariantCultureIgnoreCase;

        static StringComparer StringComparerNoCaseNoSymbol { get; set; } = StringComparer.InvariantCultureIgnoreCase;

        static StringComparer StringComparerCaseSymbol { get; set; } = StringComparer.InvariantCulture;

        static StringComparer StringComparerCaseNoSymbol { get; set; } = StringComparer.InvariantCulture;

        #endregion

        #region Constructor
        /// <summary>
        /// Basic class initialization.
        /// </summary>
        public static void Init(IHash hash)
        {
            // Case insensitive, whitespace/symbol sensitive
            StringComparerNoCaseSymbol = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hash.HashFunction);

            // Case insensitive, whitespace/symbol insensitive
            StringComparerNoCaseNoSymbol = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hash.HashFunction);

            // Case sensitive, whitespace/symbol sensitive.
            StringComparerCaseSymbol = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hash.HashFunction);

            // Case sensitive, whitespace/symbol insensitive.
            StringComparerCaseNoSymbol = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hash.HashFunction);

            // Default is fully insensitive
            StringComparer = StringComparerNoCaseNoSymbol;
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// A string comparer object that allows comparison between strings that
        /// can ignore lots of annoying user-entered variances.
        /// </summary>
        public static StringComparer StringComparer { get; private set; } = StringComparer.InvariantCultureIgnoreCase;

        /// <summary>
        /// Gets a string comparer object that ignores case and symbols.
        /// </summary>
        public static StringComparer InsensitiveComparer => StringComparerNoCaseNoSymbol;

        /// <summary>
        /// Gets a string comparer object based on the sensitivity settings of the currently selected quest.
        /// </value>
        public static StringComparer QuestSensitiveStringComparer(IQuest quest)
        {
            if (quest != null)
            {
                if (quest.WhitespaceAndPunctuationIsSignificant)
                {
                    if (quest.CaseIsSignificant)
                    {
                        return StringComparerCaseSymbol;
                    }
                    else
                    {
                        return StringComparerNoCaseSymbol;
                    }
                }
                else
                {
                    if (quest.CaseIsSignificant)
                    {
                        return StringComparerCaseNoSymbol;
                    }
                    else
                    {
                        return StringComparerNoCaseNoSymbol;
                    }
                }

            }

            return StringComparer;
        }

        /// <summary>
        /// Handles the PropertyChanged event of the Main View Model, watching for changes in the
        /// options of the currently selected quest.
        /// If whitespace handling changes, update the current comparer.
        /// </summary>
        /// <param name="mainViewModel">The view model that allows us to check the current quest's options.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        public void ComparisonPropertyChanged(IQuest quest, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.EndsWith("WhitespaceAndPunctuationIsSignificant") || e.PropertyName.EndsWith("CaseIsSignificant"))
            {
                if (quest.WhitespaceAndPunctuationIsSignificant == true && quest.CaseIsSignificant == false)
                {
                    StringComparer = StringComparerNoCaseSymbol;
                }
                else if (quest.WhitespaceAndPunctuationIsSignificant == false && quest.CaseIsSignificant == false)
                {
                    StringComparer = StringComparerNoCaseNoSymbol;
                }
                else if (quest.WhitespaceAndPunctuationIsSignificant == true && quest.CaseIsSignificant == true)
                {
                    StringComparer = StringComparerCaseSymbol;
                }
                else if (quest.WhitespaceAndPunctuationIsSignificant == false && quest.CaseIsSignificant == true)
                {
                    StringComparer = StringComparerCaseNoSymbol;
                }
            }
        }

        #endregion Public Interface

    }
}
