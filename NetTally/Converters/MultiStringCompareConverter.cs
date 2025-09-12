using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using NetTally.Tally.Vote.Comparers;
using NetTally.Tally.Vote.Component;

namespace NetTally.Converters
{
    /// <summary>
    /// Data binding conversion class to return whether all strings passed
    /// in via the values array are the same.
    /// </summary>
    public class MultiStringCompareConverter : IMultiValueConverter
    {
        /// <summary>
        /// Return a bool indicating if all the string values in the array are the same.
        /// </summary>
        /// <param name="values">Collection of binding values.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">Optional "Invert" to reverse the results.</param>
        /// <param name="culture"></param>
        /// <returns>Returns true if any bindings are true.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0 || values.Any(v => v is null))
                return false;

            return parameter?.ToString() switch
            {
                "VoteLineBlock" => CompareVoteLineBlockValues(values, inverted: false),
                "InvertVoteLineBlock" => CompareVoteLineBlockValues(values, inverted: true),
                _ => CompareStringValues(values)
            };
        }

        private static object CompareStringValues(object[] values)
        {
            string first = values[0].ToString() ?? "";

            return values.All(v => v is string vv && vv == first);
        }

        private static object CompareVoteLineBlockValues(object[] values, bool inverted)
        {
            if (!values.All(v => v is VoteBlock))
                return false;

            return inverted ^ 
                (values[0] is VoteBlock first &&
                 values.All(v =>
                    v is VoteBlock value &&
                    VoteBlockComparer.Instance.Equals(value, first)));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("MultiStringCompareConverter is a one-way converter.");
        }
    }
}
