using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
        /// Initialize the agnostic string comparers using the provided hash function.
        /// Injects the function from the non-PCL assembly, to get around PCL limitations.
        /// MUST be run before other objects are constructed.
        /// </summary>
        /// <param name="hashFunction"></param>
        public static PropertyChangedEventHandler HashStringsUsing(Func<string, CompareInfo, CompareOptions, int> hashFunction)
        {
            hashFunction = hashFunction ?? DefaultAgnosticHashFunction.HashFunction;

            StringComparer1 = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hashFunction);
            StringComparer2 = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hashFunction);

            currentComparer = StringComparer2;

            // This function is called by the MainViewModel during construction. Return the event handler we want to attach.
            return MainViewModel_PropertyChanged;
        }

        private static IEqualityComparer<string> currentComparer;

        private static IEqualityComparer<string> StringComparer1 { get; set; }

        private static IEqualityComparer<string> StringComparer2 { get; set; }

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
                if (e.PropertyName.EndsWith("WhitespaceAndPunctuationIsSignificant"))
                {
                    currentComparer = ViewModelService.MainViewModel.SelectedQuest?.WhitespaceAndPunctuationIsSignificant ?? false ? StringComparer1 : StringComparer2;
                }
            }
        }
    }
}
