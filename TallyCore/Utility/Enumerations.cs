using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace NetTally
{
    /// <summary>
    /// Enum for separating vote categories
    /// </summary>
    public enum VoteType
    {
        Vote,
        Plan,
        Rank
    }

    /// <summary>
    /// Enum for various modes of constructing the final tally display.
    /// </summary>
    public enum DisplayMode
    {
        [Description("Normal")]
        Normal,
        [Description("Spoiler Voters")]
        SpoilerVoters,
        [Description("Spoiler All")]
        SpoilerAll,
        [Description("Compact")]
        Compact
    }

    /// <summary>
    /// Enum for various modes of constructing the final tally display.
    /// </summary>
    public enum PartitionMode
    {
        [Description("No Partitioning")]
        None,
        [Description("Partition By Line")]
        ByLine,
        [Description("Partition By Block")]
        ByBlock,
        [Description("Partition By Task")]
        ByTask,
        [Description("Partition By Task/Block")]
        ByTaskBlock
    }

    public static class Enumerations
    {
        // Regex for control and formatting characters that we don't want to allow processing of.
        // EG: \u200B, non-breaking space
        // Do not remove CR/LF characters
        public static Regex UnsafeCharsRegex { get; } = new Regex(@"[\p{C}-[\r\n]]");

        /// <summary>
        /// Filter unsafe characters from the provided string.
        /// </summary>
        /// <param name="input">The string to filter.</param>
        /// <returns>The input string with all unicode control characters (except cr/lf) removed.</returns>
        public static string SafeString(string input)
        {
            return UnsafeCharsRegex.Replace(input, "");
        }

        public static string PlanNameMarker { get; } = "\u25C8";

        /// <summary>
        /// Gets a user-friendly string description of an enum value.
        /// </summary>
        /// <typeparam name="T">An enum type.</typeparam>
        /// <param name="enumerationValue">The enum we're working on.</param>
        /// <returns>Returns the string description of the enum, as provided by attributes
        /// in the original definition.</returns>
        public static string GetDescription(this Enum enumerationValue)
        {
            FieldInfo fi = enumerationValue.GetType().GetField(enumerationValue.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
                return attributes[0].Description;
            else
                return enumerationValue.ToString();
        }

        /// <summary>
        /// Gets an enum value from a provided description.
        /// </summary>
        /// <typeparam name="T">The enum type being examined.</typeparam>
        /// <param name="description">The text description we're trying to find an enum for.</param>
        /// <returns>Returns the enum matching the description, or the default enum value.</returns>
        public static T GetValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum)
                throw new InvalidOperationException();

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;

                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }

            //throw new ArgumentException("Not found.", nameof(description));
            return default(T);
        }


        public static IEnumerable<T> EnumToList<T>()
        {
            Type enumType = typeof(T);

            // Can't use generic type constraints on value types,
            // so have to do check like this
            if (enumType.BaseType != typeof(Enum))
                throw new ArgumentException("T must be of type System.Enum");

            Array enumValArray = Enum.GetValues(enumType);

            List<T> list = new List<T>();

            foreach (T val in enumValArray)
                list.Add(val);

            return list;
        }

        public static IEnumerable<string> EnumDescriptionsList<T>()
        {
            var enums = EnumToList<T>();

            var enumDescrips = from Enum e in enums
                               select e.GetDescription();

            return enumDescrips;
        }
    }
}
