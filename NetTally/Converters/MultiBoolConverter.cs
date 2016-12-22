using System;
using System.Globalization;
using System.Windows.Data;

namespace NetTally.Converters
{
    /// <summary>
    /// Data binding conversion class to return the AND state of all the objects
    /// passed in via the values array.
    /// If the parameter provided is "Invert", it will return the negation of
    /// the default result.
    /// </summary>
    public class MultiBoolAndConverter : IMultiValueConverter
    {
        /// <summary>
        /// Return a bool indicating if all bool values in the array are true.
        /// </summary>
        /// <param name="values">Collection of binding values.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">Optional "Invert" to reverse the results. Changes the test to seeing if all values are false.</param>
        /// <param name="culture"></param>
        /// <returns>Returns true if all bindings are true (or false, if inverted).</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 'All' of no entries is always true.
            if (values == null || values.Length == 0)
                return true;

            bool invert = (parameter is string p && p == "Invert");

            // This will throw an exception in any typecast error.
            foreach (bool value in values)
            {
                // If we ever encounter a false value, the And will be false (or true, if inverted)
                if (value == false)
                {
                    return invert;
                }
            }

            return !invert;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("MultiBoolAndConverter is a one-way converter.");
        }
    }

    /// <summary>
    /// Data binding conversion class to return the OR state of all the objects
    /// passed in via the values array.
    /// If the parameter provided is "Invert", it will return the negation of
    /// the default result.
    /// </summary>
    public class MultiBoolOrConverter : IMultiValueConverter
    {
        /// <summary>
        /// Return a bool indicating if any bool values passed in the array are true.
        /// </summary>
        /// <param name="values">Collection of binding values.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">Optional "Invert" to reverse the results.  If any bool values are false, return true.</param>
        /// <param name="culture"></param>
        /// <returns>Returns true if any bindings are true (or false, if inverted).</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 'Any' of no entries is always false.
            if (values == null || values.Length == 0)
                return false;

            bool invert = (parameter is string p && p == "Invert");

            // This will throw an exception in any typecast error.
            foreach (bool value in values)
            {
                // If we ever encounter a true value, the Or will be true (or false, if inverted)
                if (value == true)
                {
                    return !invert;
                }
            }

            return invert;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("MultiBoolOrConverter is a one-way converter.");
        }
    }

    /// <summary>
    /// Data binding conversion class to return the AND state of all the objects
    /// passed in via the values array.
    /// If the parameter provided is "Invert", it will return the negation of
    /// the default result.
    /// </summary>
    public class MultiBoolAllTrueConverter : IMultiValueConverter
    {
        /// <summary>
        /// Return a bool indicating if all bool values in the array are true.
        /// </summary>
        /// <param name="values">Collection of binding values.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">Optional "Invert" to reverse the results. Changes the test to seeing if all values are false.</param>
        /// <param name="culture"></param>
        /// <returns>Returns true if all bindings are true (or false, if inverted).</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 'All' of no entries is always true.
            if (values == null || values.Length == 0)
                return true;

            // This will throw an exception in any typecast error.
            foreach (bool value in values)
            {
                // If we ever encounter the 'wrong' value, the entire AND is considered false.
                if (value == false)
                    return false;
            }

            // If we didn't fail, the combined result is true.
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("MultiBoolAllTrueConverter is a one-way converter.");
        }
    }

    /// <summary>
    /// Data binding conversion class to return the AND state of all the objects
    /// passed in via the values array.
    /// If the parameter provided is "Invert", it will return the negation of
    /// the default result.
    /// </summary>
    public class MultiBoolAllNotTrueConverter : IMultiValueConverter
    {
        /// <summary>
        /// Return a bool indicating if all bool values in the array are true.
        /// </summary>
        /// <param name="values">Collection of binding values.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">Optional "Invert" to reverse the results. Changes the test to seeing if all values are false.</param>
        /// <param name="culture"></param>
        /// <returns>Returns true if all bindings are true (or false, if inverted).</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 'All' of no entries is always true.
            if (values == null || values.Length == 0)
                return true;

            // This will throw an exception in any typecast error.
            foreach (bool value in values)
            {
                // If we ever encounter the 'wrong' value, the entire AND is considered false.
                if (value == true)
                    return false;
            }

            // If we didn't fail, the combined result is true.
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("MultiBoolAllNotTrueConverter is a one-way converter.");
        }
    }

    /// <summary>
    /// Data binding conversion class to return the OR state of all the objects
    /// passed in via the values array.
    /// If the parameter provided is "Invert", it will return the negation of
    /// the default result.
    /// </summary>
    public class MultiBoolAnyTrueConverter : IMultiValueConverter
    {
        /// <summary>
        /// Return a bool indicating if any bool values passed in the array are true.
        /// </summary>
        /// <param name="values">Collection of binding values.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">Optional "Invert" to reverse the results.  If any bool values are false, return true.</param>
        /// <param name="culture"></param>
        /// <returns>Returns true if any bindings are true (or false, if inverted).</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 'Any' of no entries is always false.
            if (values == null || values.Length == 0)
                return false;

            // This will throw an exception in any typecast error.
            foreach (bool value in values)
            {
                // If we ever encounter the sought value, we succeeded.
                if (value == true)
                    return true;
            }

            // If we didn't encounter the sought value, we failed.
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("MultiBoolAnyTrueConverter is a one-way converter.");
        }
    }

    /// <summary>
    /// Data binding conversion class to return the OR state of all the objects
    /// passed in via the values array.
    /// If the parameter provided is "Invert", it will return the negation of
    /// the default result.
    /// </summary>
    public class MultiBoolAnyNotTrueConverter : IMultiValueConverter
    {
        /// <summary>
        /// Return a bool indicating if any bool values passed in the array are true.
        /// </summary>
        /// <param name="values">Collection of binding values.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">Optional "Invert" to reverse the results.  If any bool values are false, return true.</param>
        /// <param name="culture"></param>
        /// <returns>Returns true if any bindings are true (or false, if inverted).</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 'Any' of no entries is always false.
            if (values == null || values.Length == 0)
                return false;

            // This will throw an exception in any typecast error.
            foreach (bool value in values)
            {
                // If we ever encounter the sought value, we succeeded.
                if (value == false)
                    return true;
            }

            // If we didn't encounter the sought value, we failed.
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("MultiBoolAnyNotTrueConverter is a one-way converter.");
        }
    }


}
