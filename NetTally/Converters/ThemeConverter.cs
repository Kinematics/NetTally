using System;
using System.Globalization;
using System.Windows.Data;
using NetTally.Enums;

namespace NetTally.Converters;

[ValueConversion(typeof(WPFTheme), typeof(int))]
public class ThemeConverter : IValueConverter
{
    /// <summary>
    /// Convert from source (property enum) to target (control index).
    /// </summary>
    /// <returns>Returns whether the specified target control value should be on or off.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is WPFTheme theme)
        {
            return (int)theme;
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
        if (value is int themeIndex && Enum.IsDefined(typeof(WPFTheme), themeIndex))
        {
            return (WPFTheme)themeIndex;
        }

        return WPFTheme.System;
    }
}