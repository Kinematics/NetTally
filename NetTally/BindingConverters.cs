using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace NetTally
{
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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter.Equals("Invert"))
                return !(bool)value;
            else
                return value;
        }
    }


    /// <summary>
    /// Data binding conversion class to return the AND state of all the objects
    /// passed in via the values array.
    /// If the parameter provided is "Invert", it will return the negation of
    /// the expected result.
    /// </summary>
    public class MultiBoolAndConverter : IMultiValueConverter
    {
        /// <summary>
        /// Return a bool indicating if all bool values passed in the array are true.
        /// </summary>
        /// <param name="values">Collection of binding values.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">Optional "Invert" to reverse the results.</param>
        /// <param name="culture"></param>
        /// <returns>Returns true if all bindings are true.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool pass = true;
            if (parameter != null && parameter.Equals("Invert"))
                pass = false;

            foreach (object value in values)
            {
                if ((value is bool) && (bool)value == false)
                {
                    return !pass;
                }
            }
            return pass;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("MultiBoolConverter is a OneWay converter.");
        }
    }

    /// <summary>
    /// Data binding conversion class to return the OR state of all the objects
    /// passed in via the values array.
    /// If the parameter provided is "Invert", it will return the negation of
    /// the expected result.
    /// </summary>
    public class MultiBoolOrConverter : IMultiValueConverter
    {
        /// <summary>
        /// Return a bool indicating if any bool values passed in the array are true.
        /// </summary>
        /// <param name="values">Collection of binding values.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">Optional "Invert" to reverse the results.</param>
        /// <param name="culture"></param>
        /// <returns>Returns true if any bindings are true.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool pass = true;
            if (parameter != null && parameter.Equals("Invert"))
                pass = false;

            foreach (object value in values)
            {
                if ((value is bool) && (bool)value == true)
                {
                    return pass;
                }
            }
            return !pass;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("MultiBoolConverter is a OneWay converter.");
        }
    }


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
            if (value is bool)
            {
                bool visible = (bool)value;
                if (parameter != null && parameter.Equals("Invert"))
                    visible = !visible;

                if (visible)
                    return Visibility.Visible;
                else
                    return Visibility.Hidden;
            }

            return Visibility.Hidden;
        }

        /// <summary>
        /// Convert from target (control bool) to source (property bool).
        /// </summary>
        /// <returns>Returns what the source property value should be set to
        /// based on the target value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("BoolToVisibilityConverter is a OneWay converter.");
        }
    }


    /// <summary>
    /// Data binding conversion class to convert a DisplayMode enum to
    /// an index value or back.
    /// </summary>
    [ValueConversion(typeof(DisplayMode), typeof(int))]
    public class DisplayModeConverter : IValueConverter
    {
        /// <summary>
        /// Convert from source (property enum) to target (control index).
        /// </summary>
        /// <returns>Returns whether the specified target control value should be on or off.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DisplayMode)
            {
                return (int)value;
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
            if (value is int)
            {
                return (DisplayMode)value;
            }

            return DisplayMode.Normal;
        }
    }

    /// <summary>
    /// Data binding conversion class to convert a PartitionMode enum to
    /// an index value or back.
    /// </summary>
    [ValueConversion(typeof(PartitionMode), typeof(int))]
    public class PartitionModeConverter : IValueConverter
    {
        /// <summary>
        /// Convert from source (property enum) to target (control index).
        /// </summary>
        /// <returns>Returns whether the specified target control value should be on or off.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PartitionMode)
            {
                return (int)value;
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
            if (value is int)
            {
                return (PartitionMode)value;
            }

            return PartitionMode.None;
        }
    }


    /// <summary>
    /// Data binding conversion class to return the OR state of all the objects
    /// passed in via the values array.
    /// If the parameter provided is "Invert", it will return the negation of
    /// the expected result.
    /// </summary>
    public class MultiStringCompareConverter : IMultiValueConverter
    {
        /// <summary>
        /// Return a bool indicating if any bool values passed in the array are true.
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

            string first = values[0].ToString();

            return values.All(v => v is string && (string)v == first);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("MultiStringCompareConverter is a OneWay converter.");
        }
    }
}
