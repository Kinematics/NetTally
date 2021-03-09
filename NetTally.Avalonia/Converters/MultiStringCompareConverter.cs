using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using NetTally.Votes;

namespace NetTally.Avalonia.Converters
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
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
                return false;
            if (values.Count == 0)
                return false;
            if (values.Any(v => v == null))
                return false;

            if (parameter.ToString() == "VoteLineBlock")
                return CompareVoteLineBlockValues(values, inverted: false);
            else if (parameter.ToString() == "InvertVoteLineBlock")
                return CompareVoteLineBlockValues(values, inverted: true);
            else
                return CompareStringValues(values);
        }

        private object CompareStringValues(IList<object> values)
        {
            string first = values[0].ToString() ?? "";

            return values.All(v => v is string vv && vv == first);
        }
        private object CompareVoteLineBlockValues(IList<object> values, bool inverted)
        {
            if (!values.All(v => v is VoteLineBlock))
                return false;

            return inverted ^ (values[0] is VoteLineBlock first && values.All(v => v is VoteLineBlock value && value == first));
        }

        public IList<object> ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("MultiStringCompareConverter is a one-way converter.");
        }
    }
}
