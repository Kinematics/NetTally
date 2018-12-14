using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using NetTally.Comparers;
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
            HashStringsUsing(DefaultHashFunction);
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

        #region Hash Function options
        /// <summary>
        /// Craft a hash function which returns identical values for strings that
        /// may vary by case or punctuation or diacriticals or punctuation.
        /// This ensures strings which the agnostic comparer may consider the same
        /// get the same hash code, so that the full comparison is actually made.
        /// </summary>
        public static int DefaultHashFunction(string str, CompareInfo info, CompareOptions options)
        {
            var normalized = str.Normalize(NormalizationForm.FormKD).AsSpan();

            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;
                ushort ch = 0;

                for (int i = 0; i < normalized.Length; i++)
                {
                    // only hash from letters
                    if (char.IsLetterOrDigit(normalized[i]))
                    {
                        // Start hash2 at i == 1
                        if (ch > 0)
                        {
                            hash2 = ((hash2 << 5) + hash2) ^ ch;
                        }

                        // convert to lowercase if it's in ASCII range
                        ch = Convert.ToUInt16(normalized[i]);
                        if (ch > 64 && ch < 91)
                            ch += 32;

                        hash1 = ((hash1 << 5) + hash1) ^ ch;
                    }
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        /// <summary>
        /// Hashes the provided string using the given CompareOptions.
        /// Doing this allows all custom compare options to be applied in determining the hash value.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="info">The CompareInfo object doing the unicode-aware comparison.</param>
        /// <param name="options">The options to apply to the comparison.</param>
        /// <returns>Returns the hash code for the string.</returns>
        public static int AlternateUnicodeHashFunction(string str, CompareInfo info, CompareOptions options)
        {
            if (string.IsNullOrEmpty(str))
                return 0;

            SortKey sortOrder = info.GetSortKey(str, options);

            int hash = GetByteArrayHash(sortOrder.KeyData);

            return hash;
        }

        /// <summary>
        /// Generates a hash code for an array of bytes.
        /// </summary>
        /// <param name="keyData">The key data.</param>
        /// <returns>Returns a hash code value.</returns>
        private static int GetByteArrayHash(byte[] keyData)
        {
            unchecked
            {
                const int p = 16777619;
                int hash = (int)2166136261;

                for (int i = 0; i < keyData.Length; i++)
                    hash = (hash ^ keyData[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }
        #endregion


        /// <summary>
        /// Initialize the agnostic string comparers using the provided hash function.
        /// Injects the function from the non-PCL assembly, to get around PCL limitations.
        /// MUST be run before other objects are constructed.
        /// </summary>
        /// <param name="hashFunction">The hash function to use for the various forms of string comparer.</param>
        public static PropertyChangedEventHandler HashStringsUsing(Func<string, CompareInfo, CompareOptions, int> hashFunction)
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

            // This function is called by the MainViewModel during construction. Return the event handler we want to attach.
            return MainViewModel_PropertyChanged;
        }

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
        public static string? AgnosticMatch(this IEnumerable<string> self, string value)
        {
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
        public static string? AgnosticMatch(this string value, IEnumerable<string> list)
        {
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
