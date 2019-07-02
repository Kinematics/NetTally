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
        private static IEqualityComparer<string> currentComparer;

        private static IEqualityComparer<string> StringComparerNoCaseSymbol { get; set; }

        private static IEqualityComparer<string> StringComparerNoCaseNoSymbol { get; set; }

        private static IEqualityComparer<string> StringComparerCaseSymbol { get; set; }

        private static IEqualityComparer<string> StringComparerCaseNoSymbol { get; set; }

        /// <summary>
        /// A string comparer object that allows comparison between strings that
        /// can ignore lots of annoying user-entered variances.
        /// </summary>
        public static IEqualityComparer<string> StringComparer => currentComparer;

        /// <summary>
        /// Gets a string comparer object that ignores case and symbols.
        /// </summary>
        public static IEqualityComparer<string> InsensitiveComparer => StringComparerNoCaseNoSymbol;

        /// <summary>
        /// Gets a string comparer object based on the sensitivity settings of the currently selected quest.
        /// </value>
        public static IEqualityComparer<string> QuestSensitiveStringComparer
        {
            get
            {
                if (ViewModelService.MainViewModel?.SelectedQuest is IQuest quest)
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

                return currentComparer;
            }
        }
        #endregion

        /// <summary>
        /// Initialize the agnostic string comparers using the provided hash function.
        /// Injects the function from the non-PCL assembly, to get around PCL limitations.
        /// MUST be run before other objects are constructed.
        /// </summary>
        /// <param name="hashFunction">The hash function to use for the various forms of string comparer.</param>
        public static void HashStringsUsing(Func<string, CompareInfo, CompareOptions, int> hashFunction)
        {
            StringComparerNoCaseSymbol = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hashFunction);
            StringComparerNoCaseNoSymbol = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols |  CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hashFunction);
            // Case Sensitive, Symbols Sensitive.
            StringComparerCaseSymbol = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hashFunction);
            // Case Sensitive, Symbols No Sensitive.
            StringComparerCaseNoSymbol = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hashFunction);

            currentComparer = StringComparerNoCaseNoSymbol;
        }

        /// <summary>
        /// Handles the PropertyChanged event of the Main View Model, watching for changes in the
        /// options of the currently selected quest.
        /// If whitespace handling changes, update the current comparer.
        /// </summary>
        /// <param name="mainViewModel">The view model that allows us to check the current quest's options.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        public static void ComparisonPropertyChanged(MainViewModel mainViewModel, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.StartsWith("SelectedQuest."))
            {
                if (e.PropertyName.EndsWith("WhitespaceAndPunctuationIsSignificant") || e.PropertyName.EndsWith("CaseIsSignificant"))
                {
                    if (mainViewModel?.SelectedQuest?.WhitespaceAndPunctuationIsSignificant == true && mainViewModel?.SelectedQuest?.CaseIsSignificant == false)
                    {
                        currentComparer = StringComparerNoCaseSymbol;
                    }
                    else if (mainViewModel?.SelectedQuest?.WhitespaceAndPunctuationIsSignificant == false && mainViewModel?.SelectedQuest?.CaseIsSignificant == false)
                    {
                        currentComparer = StringComparerNoCaseNoSymbol;
                    }
                    else if (mainViewModel?.SelectedQuest?.WhitespaceAndPunctuationIsSignificant == true && mainViewModel?.SelectedQuest?.CaseIsSignificant == true)
                    {
                        currentComparer = StringComparerCaseSymbol;
                    }
                    else if (mainViewModel?.SelectedQuest?.WhitespaceAndPunctuationIsSignificant == false && mainViewModel?.SelectedQuest?.CaseIsSignificant == true)
                    {
                        currentComparer = StringComparerCaseNoSymbol;
                    }
                    else { System.Diagnostics.Debug.WriteLine($"Invalid Compare/Whitespace options set."); }
                }
            }
        }

        /// <summary>
        /// Find the first character difference between two strings.
        /// </summary>
        /// <param name="first">First string.</param>
        /// <param name="second">Second string.</param>
        /// <returns>Returns the index of the first difference between the strings.  -1 if they're equal.</returns>
        public static int FirstDifferenceInStrings(string input1, string input2)
        {
            ReadOnlySpan<char> first = input1.AsSpan();
            ReadOnlySpan<char> second = input2.AsSpan();

            int length = first.Length < second.Length ? first.Length : second.Length;

            for (int i = 0; i < length; i++)
            {
                if (first[i] != second[i])
                    return i;
            }

            if (first.Length != second.Length)
                return Math.Min(first.Length, second.Length);

            return -1;
        }
    }
}
