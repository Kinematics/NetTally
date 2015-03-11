using System;
using System.Globalization;
using System.Windows.Data;

namespace NetTally
{
    /// <summary>
    /// Data binding conversion class to convert a Quest object to the
    /// name of that quest.
    /// </summary>
    [ValueConversion(typeof(IQuest), typeof(string))]
    public class QuestConverter : IValueConverter
    {
        /// <summary>
        /// Convert from source (Quest) to target (string).
        /// </summary>
        /// <returns>Returns the name of the quest object for display.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var q = value as IQuest;
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
            return Quests.StaticGetQuestByName((string)value);
        }
    }
    


    /// <summary>
    /// Data binding conversion class to convert the bool to either the same value,
    /// or the inverse of the value, depending on the parameter value.
    /// The 'parameter' parameter specifies whether to invert the boolean value.
    /// Value of "Invert" will cause it to return the negation of the bool.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BoolConverter : IValueConverter
    {
        /// <summary>
        /// Convert from source (property bool) to target (control bool).
        /// </summary>
        /// <returns>Returns whether the specified target control value should be on or off.</returns>
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (parameter.Equals("Invert"))
                return !(bool)value;
            else
                return value;
        }

        /// <summary>
        /// Convert from target (control bool) to source (property bool).
        /// </summary>
        /// <returns>Returns what the source property value should be set to
        /// based on the target value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (parameter.Equals("Invert"))
                return !(bool)value;
            else
                return value;
        }
    }


}
