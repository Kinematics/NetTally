using System;
using System.Globalization;
using System.Windows.Data;

namespace NetTally
{
    /// <summary>
    /// Data binding conversion class to convert a Quest object to the
    /// name of that quest.
    /// </summary>
    [ValueConversion(typeof(Quest), typeof(string))]
    public class QuestConverter : IValueConverter
    {
        /// <summary>
        /// Convert from source (Quest) to target (string).
        /// </summary>
        /// <returns>Returns the name of the quest object for display.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var q = value as Quest;
            if (q != null)
                return q.Name;

            return string.Empty;
        }

        /// <summary>
        /// Convert from target (string) to source (Quest).
        /// </summary>
        /// <returns>Returns the object that corresponds to the provided quest name.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Quests.GetQuest((string)value);
        }
    }


    /// <summary>
    /// Data binding conversion class to convert the bool value of a property to
    /// the bool value of a specified radio button.
    /// The 'parameter' parameter specifies which radio button is being set, so
    /// that the bool property can be adjusted to match.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class RadioPartitionConverter : IValueConverter
    {
        /// <summary>
        /// Convert from source (property bool) to target (radiobutton bool).
        /// </summary>
        /// <returns>Returns whether the specified radiobutton should be on or off.</returns>
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (parameter.Equals("Line"))
                return value;
            else
                return !(bool)value;
        }

        /// <summary>
        /// Convert from target (radiobutton bool) to source (property bool).
        /// </summary>
        /// <returns>Returns the value the property should be set to based on the
        /// current radio button value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (parameter.Equals("Line"))
                return value;
            else
                return !(bool)value;
        }
    }
}
