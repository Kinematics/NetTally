using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using NetTally.Comparers;
using NetTally.ViewModels;

namespace NetTally.Utility
{
    public static class Agnostic
    {
        internal static class DefaultAgnosticHashFunction
        {
            /// <summary>
            /// Always return 0, since we can't assume that we'll have access to UTF comparer functions,
            /// which means we can't properly distinguish between different types of graphemes.
            /// </summary>
            public static int HashFunction(string str, CompareInfo info, CompareOptions options)
            {
                return 0;
            }
        }

        /// <summary>
        /// A string comparer object that allows comparison between strings that
        /// can ignore lots of annoying user-entered variances.
        /// </summary>
        public static IEqualityComparer<string> StringComparer
        {
            get
            {
                if (currentComparer == null)
                    HashStringsUsing(null);

                return currentComparer;
            }
        }

        /// <summary>
        /// Gets a string comparer object based on the sensitivity settings of the currently selected quest.
        /// </value>
        public static IEqualityComparer<string> QuestSensitiveStringComparer
        {
            get
            {
                if (currentComparer == null)
                    HashStringsUsing(null);

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

        /// <summary>
        /// Gets a string comparer object that ignores case and symbols.
        /// </summary>
        public static IEqualityComparer<string> InsensitiveComparer => StringComparerNoCaseNoSymbol;

        /// <summary>
        /// Initialize the agnostic string comparers using the provided hash function.
        /// Injects the function from the non-PCL assembly, to get around PCL limitations.
        /// MUST be run before other objects are constructed.
        /// </summary>
        /// <param name="hashFunction"></param>
        public static PropertyChangedEventHandler HashStringsUsing(Func<string, CompareInfo, CompareOptions, int> hashFunction)
        {
            hashFunction = hashFunction ?? DefaultAgnosticHashFunction.HashFunction;

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

            // This function is called by the MainViewModel during construction. Return the event handler we want to attach.
            return MainViewModel_PropertyChanged;
        }

        private static IEqualityComparer<string> currentComparer;

        private static IEqualityComparer<string> StringComparerNoCaseSymbol { get; set; }

        private static IEqualityComparer<string> StringComparerNoCaseNoSymbol { get; set; }

        private static IEqualityComparer<string> StringComparerCaseSymbol { get; set; }

        private static IEqualityComparer<string> StringComparerCaseNoSymbol { get; set; }

        /// <summary>
        /// Handles the PropertyChanged event of the Main View Model, watching for changes in the
        /// options of the currently selected quest.
        /// If whitespace handling changes, update the current comparer.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.StartsWith("SelectedQuest."))
            {
                if (e.PropertyName.EndsWith("WhitespaceAndPunctuationIsSignificant") || e.PropertyName.EndsWith("CaseIsSignificant"))
                {
                    if (ViewModelService.MainViewModel.SelectedQuest?.WhitespaceAndPunctuationIsSignificant == true && ViewModelService.MainViewModel.SelectedQuest?.CaseIsSignificant == false)
                    {
                        currentComparer = StringComparerNoCaseSymbol;
                    }
                    else if (ViewModelService.MainViewModel.SelectedQuest?.WhitespaceAndPunctuationIsSignificant == false && ViewModelService.MainViewModel.SelectedQuest?.CaseIsSignificant == false)
                    {
                        currentComparer = StringComparerNoCaseNoSymbol;
                    }
                    else if (ViewModelService.MainViewModel.SelectedQuest?.WhitespaceAndPunctuationIsSignificant == true && ViewModelService.MainViewModel.SelectedQuest?.CaseIsSignificant == true)
                    {
                        currentComparer = StringComparerCaseSymbol;
                    }
                    else if (ViewModelService.MainViewModel.SelectedQuest?.WhitespaceAndPunctuationIsSignificant == false && ViewModelService.MainViewModel.SelectedQuest?.CaseIsSignificant == true)
                    {
                        currentComparer = StringComparerCaseNoSymbol;
                    }
                    else { System.Diagnostics.Debug.WriteLine($"Invalid Compare/Whitespace options set."); }
                }
            }
        }


        /// <summary>
        /// Returns the first match within the enumerable list that agnostically
        /// equals the provided value.
        /// Extends the enumerable.
        /// </summary>
        /// <param name="self">The list to search.</param>
        /// <param name="value">The value to compare with.</param>
        /// <returns>Returns the item in the list that matches the value, or null.</returns>
        public static string AgnosticMatch(this IEnumerable<string> self, string value)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            foreach (string item in self)
            {
                if (Agnostic.StringComparer.Equals(item, value))
                    return item;
            }

            return null;
        }

        /// <summary>
        /// Returns the first match within the enumerable list that agnostically
        /// equals the provided value.
        /// Extends a string.
        /// </summary>
        /// <param name="value">The value to compare with.</param>
        /// <param name="list">The list to search.</param>
        /// <returns>Returns the item in the list that matches the value, or null.</returns>
        public static string AgnosticMatch(this string value, IEnumerable<string> list)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            foreach (string item in list)
            {
                if (Agnostic.StringComparer.Equals(item, value))
                    return item;
            }

            return null;
        }

        /// <summary>
        /// Find the first character difference between two strings.
        /// </summary>
        /// <param name="first">First string.</param>
        /// <param name="second">Second string.</param>
        /// <returns>Returns the index of the first difference between the strings.  -1 if they're equal.</returns>
        public static int FirstDifferenceInStrings(this string first, string second)
        {
            if (first == null)
                throw new ArgumentNullException(nameof(first));
            if (second == null)
                throw new ArgumentNullException(nameof(second));

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
