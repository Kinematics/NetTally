using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace NetTally.Avalonia.Converters
{
    /// <summary>
    /// Data binding conversion class to convert a PartitionMode enum to
    /// an index value or back.
    /// </summary>
    public class EnumConverter : IValueConverter
    {
        /// <summary>
        /// Gets a static instance of this converter.
        /// </summary>
        public static IValueConverter Instance { get; private set; }

        /// <summary>
        /// Initializes static members of the <see cref="EnumConverter"/> class.
        /// </summary>
        static EnumConverter() => Instance = new EnumConverter();

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumConverter"/> class.
        /// </summary>
        /// <remarks>Private because this class should only be created by the static constructor.</remarks>
        private EnumConverter() { }

        /// <summary>
        /// Convert from source enum to target index (int).
        /// </summary>
        /// <returns>Returns the control index of the given enum, or -1 if invalid.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Enum e)
            {
                return System.Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType()));
            }

            return -1;
        }

        /// <summary>
        /// Convert from index (int) to source enum.
        /// </summary>
        /// <returns>Returns the enum option for that index, or the default value (0) if invalid.</returns>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (targetType.IsEnum && value is not null)
            {
                return Enum.ToObject(targetType, value);
            }

            return Enum.ToObject(targetType, 0);
        }
    }
}
