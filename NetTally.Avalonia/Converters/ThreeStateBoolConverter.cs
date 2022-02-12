using Avalonia.Data.Converters;
using NetTally.Types.Enums;
using System;
using System.Globalization;

namespace NetTally.Avalonia.Converters
{
    public class ThreeStateBoolConverter : IValueConverter
    {
        /// <summary>
        /// Convert from source (property BoolEx) to target (control bool?).
        /// </summary>
        /// <returns>Returns whether the specified target control value should be on or off.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                BoolEx b => b switch
                {
                    BoolEx.True => true,
                    BoolEx.False => false,
                    _ => null,
                },
                _ => throw new ArgumentException("Value is not a BoolEx."),
            };
        }

        /// <summary>
        /// Convert from target (control bool?) to source (property BoolEx).
        /// </summary>
        /// <returns>Returns what the source property value should be set to based on the target value.</returns>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value switch
            {
                null => BoolEx.Unknown,
                bool b => b ? BoolEx.True : BoolEx.False,
                _ => throw new ArgumentException("Value is not a bool."),
            };
        }
    }
}
