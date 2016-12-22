using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NetTally.Converters
{
    /// <summary>
    /// Data binding conversion class to convert the bool to either the same value,
    /// or the inverse of the value, depending on the parameter value.
    /// The 'parameter' parameter specifies whether to invert the boolean value.
    /// Value of "Invert" will cause it to return the negation of the bool.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convert from source (property bool) to target (visibility of the control).
        /// </summary>
        /// <returns>Returns whether the specified target control value should be on or off.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool visible)
            {
                if (parameter is string p && p == "Invert")
                    visible = !visible;

                if (visible)
                    return Visibility.Visible;
                else
                    return Visibility.Hidden;
            }

            return Visibility.Hidden;
        }

        /// <summary>
        /// Convert from target (visibility of the control) to source (property bool).
        /// </summary>
        /// <returns>Returns what the source property value should be set to
        /// based on the target value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visible)
            {
                bool invert = (parameter is string p && p == "Invert");

                switch (visible)
                {
                    case Visibility.Visible:
                        return !invert;
                    case Visibility.Hidden:
                        return invert;
                    case Visibility.Collapsed:
                        return invert;
                }
            }

            return false;
        }
    }
}
