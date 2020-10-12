using System;
using System.Globalization;
using Avalonia.Data.Converters;
using NetTally.Output;

namespace NetTally.Avalonia.Converters
{
    /// <summary>
    /// Data binding conversion class to convert a DisplayMode enum to
    /// an index value or back.
    /// </summary>
    public class DisplayModeConverter : IValueConverter
    {
        /// <summary>
        /// Convert from source (property enum) to target (control index).
        /// </summary>
        /// <returns>Returns whether the specified target control value should be on or off.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DisplayMode dm)
            {
                return (int)dm;
            }

            return -1;
        }

        /// <summary>
        /// Convert from target (control index) to source (property enum).
        /// </summary>
        /// <returns>Returns what the source property value should be set to
        /// based on the target value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int dmv)
            {
                return (DisplayMode)dmv;
            }

            return DisplayMode.Normal;
        }
    }
}
