using System.ComponentModel;
using System.Reflection;

namespace NetTally.Utility.Enumerations;

/// <summary>
/// Static class that can be used for extension methods on enums, in order to use
/// user-friendly description attributes in the UI.
/// </summary>
static class EnumExtensions
{
    /// <summary>
    /// Gets a user-friendly string description of an enum value.
    /// </summary>
    /// <typeparam name="T">An enum type.</typeparam>
    /// <param name="enumerationValue">The enum we're working on.</param>
    /// <returns>Returns the string description of the enum, as provided by attributes
    /// in the original definition.</returns>
    public static string GetDescription(this Enum enumerationValue)
    {
        string enumString = enumerationValue.ToString();

        string enumDescription = enumerationValue
            .GetType()
            .GetFields()
            .FirstOrDefault(f => f.Name == enumString)
            ?.GetCustomAttribute<DescriptionAttribute>()
            ?.Description ?? enumString;

        return enumDescription;
    }

    /// <summary>
    /// Gets an enum value from a provided description.
    /// </summary>
    /// <typeparam name="T">The enum type being examined.</typeparam>
    /// <param name="description">The text description we're trying to find an enum for.</param>
    /// <returns>Returns the enum matching the description, or the default enum value.</returns>
    public static T GetValueFromDescription<T>(string description) where T : struct, Enum
    {
        var enumType = typeof(T);

        foreach (var fieldInfo in enumType.GetFields())
        {
            DescriptionAttribute? fieldAttribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>();

            if (fieldAttribute?.Description == description || fieldAttribute == null && fieldInfo.Name == description)
            {
                var v = fieldInfo.GetValue(null);

                if (v != null)
                    return (T)v;

                return default;
            }
        }

        return default;
    }

    /// <summary>
    /// Create a list of the descriptions of each enum value of a given type.
    /// </summary>
    /// <typeparam name="T">The enum type to create a list for.</typeparam>
    /// <returns>Returns a list of string descriptions for an enum type.</returns>
    public static IEnumerable<string> EnumDescriptionsList<T>() where T : struct, Enum
    {
        T[] enums = Enum.GetValues<T>();

        var enumDescrips = enums.Select(e => e.GetDescription());

        return enumDescrips;
    }
}
