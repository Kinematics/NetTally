using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using NetTally.Comparers;
using NetTally.Utility.Comparers;
using NetTally.ViewModels;

namespace NetTally.Utility
{
    public static class Agnostic
    {
        #region Constructor
        /// <summary>
        /// Static constructor. Initialize on first use of the class.
        /// </summary>
        static Agnostic()
        {
            HashStringsUsing(new NormalHash().HashFunction);
        }
        #endregion

        #region Fields and properties
        private static CustomStringComparer StringComparerNoCaseSymbol { get; set; }

        private static CustomStringComparer StringComparerNoCaseNoSymbol { get; set; }

        private static CustomStringComparer StringComparerCaseSymbol { get; set; }

        private static CustomStringComparer StringComparerCaseNoSymbol { get; set; }

        /// <summary>
        /// A string comparer object that allows comparison between strings that
        /// can ignore lots of annoying user-entered variances.
        /// </summary>
        public static CustomStringComparer StringComparer { get; private set; }


        /// <summary>
        /// Gets a string comparer object that ignores case and symbols.
        /// </summary>
        public static CustomStringComparer InsensitiveComparer => StringComparerNoCaseNoSymbol;

        /// <summary>
        /// Gets a string comparer object based on the sensitivity settings of the currently selected quest.
        /// </value>
        public static CustomStringComparer QuestSensitiveStringComparer(IQuest quest)
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

        public static Func<string, CompareInfo, CompareOptions, int> HashFunction { get; private set; }
        #endregion

        /// <summary>
        /// Initialize the agnostic string comparers using the provided hash function.
        /// Injects the function from the non-PCL assembly, to get around PCL limitations.
        /// MUST be run before other objects are constructed.
        /// </summary>
        /// <param name="hashFunction">The hash function to use for the various forms of string comparer.</param>
        public static void HashStringsUsing(Func<string, CompareInfo, CompareOptions, int> hashFunction)
        {
            HashFunction = hashFunction;

            // Case insensitive, whitespace/symbol sensitive
            StringComparerNoCaseSymbol = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hashFunction);
            
            // Case insensitive, whitespace/symbol insensitive
            StringComparerNoCaseNoSymbol = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols |  CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hashFunction);
            
            // Case sensitive, whitespace/symbol sensitive.
            StringComparerCaseSymbol = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hashFunction);
            
            // Case sensitive, whitespace/symbol insensitive.
            StringComparerCaseNoSymbol = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hashFunction);

            // Default is fully insensitive
            StringComparer = StringComparerNoCaseNoSymbol;
        }

        /// <summary>
        /// Handles the PropertyChanged event of the Main View Model, watching for changes in the
        /// options of the currently selected quest.
        /// If whitespace handling changes, update the current comparer.
        /// </summary>
        /// <param name="mainViewModel">The view model that allows us to check the current quest's options.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        public static void ComparisonPropertyChanged(IQuest quest, PropertyChangedEventArgs e)
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

        /// <summary>
        /// Find the first character difference between two strings.
        /// </summary>
        /// <param name="first">First string.</param>
        /// <param name="second">Second string.</param>
        /// <returns>Returns the index of the first difference between the strings.  -1 if they're equal.</returns>
        public static int FirstDifferenceInStrings(ReadOnlySpan<char> input1, ReadOnlySpan<char> input2)
        {
            int length = input1.Length < input2.Length ? input1.Length : input2.Length;

            for (int i = 0; i < length; i++)
            {
                if (input1[i] != input2[i])
                    return i;
            }

            if (input1.Length != input2.Length)
                return Math.Min(input1.Length, input2.Length);

            return -1;
        }
    }
}
