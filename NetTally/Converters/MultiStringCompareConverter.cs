using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using NetTally.Experiment3;

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
            if (values == null)
                return false;
            if (values.Length == 0)
                return false;
            if (values.Any(v => v == null))
                return false;

            if (parameter.ToString() == "VoteLineBlock")
            {
                return CompareVoteLineBlockValues(values, inverted: false);
            }
            else if (parameter.ToString() == "InvertVoteLineBlock")
            {
                return CompareVoteLineBlockValues(values, inverted: true);
            }
            else
            {
                return CompareStringValues(values);
            }
        }

        private object CompareStringValues(object[] values)
        {
            string first = values[0].ToString();

            return values.All(v => v is string vv && vv == first);
        }
        private object CompareVoteLineBlockValues(object[] values, bool inverted)
        {
            if (!values.All(v => v is VoteLineBlock))
                return false;

            return inverted ^ (values[0] is VoteLineBlock first && values.All(v => v is VoteLineBlock value && value == first));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("MultiStringCompareConverter is a one-way converter.");
        }
    }
}
