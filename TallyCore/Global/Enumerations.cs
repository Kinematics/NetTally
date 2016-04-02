using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;


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

    public enum ReferenceType
    {
        Label,
        Any,
        Voter,
        Plan,
    }

    public enum PageType
    {
        Thread,
        Threadmarks,
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
        [Description("Normal, No Voters")]
        NormalNoVoters,
        [Description("Compact")]
        Compact,
        [Description("Compact, No Voters")]
        CompactNoVoters
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
        [Description("Partition By Line (+Task)")]
        ByLineTask,
        [Description("Partition By Block")]
        ByBlock,
        [Description("Partition (Plans) By Block")]
        ByBlockAll,
    }

    /// <summary>
    /// Enum for whether to use the cache when loading a web page.
    /// </summary>
    public enum CachingMode
    {
        UseCache,
        BypassCache
    }

    public static class Enumerations
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
            if (enumerationValue == null)
                return null;

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
            if (!type.GetTypeInfo().IsEnum)
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

        /// <summary>
        /// Create a list of enums containing each of the enumerated values.
        /// </summary>
        /// <typeparam name="T">The enum type to create a list for.</typeparam>
        /// <returns>Returns an IEnumerable list of enum values.</returns>
        public static IEnumerable<T> EnumToList<T>()
        {
            Type enumType = typeof(T);

            // Can't use generic type constraints on value types,
            // so have to do check like this
            if (enumType.GetTypeInfo().BaseType != typeof(Enum))
                throw new ArgumentException("T must be of type System.Enum");

            Array enumValArray = Enum.GetValues(enumType);

            List<T> list = new List<T>();

            foreach (T val in enumValArray)
                list.Add(val);

            return list;
        }

        /// <summary>
        /// Create a list of the descriptions of each enum value of a given type.
        /// </summary>
        /// <typeparam name="T">The enum type to create a list for.</typeparam>
        /// <returns>Returns a list of string descriptions for an enum type.</returns>
        public static IEnumerable<string> EnumDescriptionsList<T>()
        {
            var enumDescrips = from Enum e in EnumToList<T>()
                               select e.GetDescription();

            return enumDescrips;
        }
    }
}
