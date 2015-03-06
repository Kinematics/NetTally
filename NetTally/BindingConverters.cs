using System;
using System.Globalization;
using System.Windows.Data;

namespace NetTally
{
    /// <summary>
    /// Converter class for data binding, to allow conversion between the name of a
    /// quest and the quest object itself.
    /// </summary>
    public class QuestConverter : IValueConverter
    {
        /// <summary>
        /// Convert from source (object) to target (string).
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
        /// Convert from target (string) to source (object).
        /// </summary>
        /// <returns>Returns the object that corresponds to the provide quest name.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Quests.GetQuest((string)value);
        }
    }


    public class RadioPartitionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (parameter.Equals("Line"))
                return value;
            else
                return !(bool)value;
        }

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
