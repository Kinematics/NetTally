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
        Rank,
        Approval
    }

    public enum RankVoteCounterMethod
    {
        [EnumDescription("Default (Coombs)")]
        Default,
        [EnumDescription("Borda")]
        BordaCount,
        [EnumDescription("Borda (Fraction)")]
        BordaFraction,
        [EnumDescription("Coombs'")]
        Coombs,
        [EnumDescription("Instant Runoff")]
        InstantRunoff,
        [EnumDescription("Schulze")]
        Schulze,
    }

    public enum StandardVoteCounterMethod
    {
        Default
    }

    public enum ApprovalVoteCounterMethod
    {
        Default
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
        [EnumDescription("Normal")]
        Normal,
        [EnumDescription("Spoiler Voters")]
        SpoilerVoters,
        [EnumDescription("Spoiler All")]
        SpoilerAll,
        [EnumDescription("Normal, No Voters")]
        NormalNoVoters,
        [EnumDescription("Compact")]
        Compact,
        [EnumDescription("Compact, No Voters")]
        CompactNoVoters
    }

    /// <summary>
    /// Enum for various modes of constructing the final tally display.
    /// </summary>
    public enum PartitionMode
    {
        [EnumDescription("No Partitioning")]
        None,
        [EnumDescription("Partition By Line")]
        ByLine,
        [EnumDescription("Partition By Line (+Task)")]
        ByLineTask,
        [EnumDescription("Partition By Block")]
        ByBlock,
        [EnumDescription("Partition (Plans) By Block")]
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

    /// <summary>
    /// Custom attribute that will be available to derived mobile projects,
    /// since the standard DescriptionAttribute is not available.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Field)]
    public class EnumDescriptionAttribute : Attribute
    {
        public string Description { get; }
        public EnumDescriptionAttribute(string description)
        {
            Description = description;
        }
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
            string enumString = enumerationValue.ToString();

            var enumInfo = enumerationValue.GetType().GetTypeInfo();
            var enumAttribute = enumInfo.GetDeclaredField(enumString)?.GetCustomAttribute<EnumDescriptionAttribute>();

            return enumAttribute?.Description ?? enumString;
        }

        /// <summary>
        /// Gets an enum value from a provided description.
        /// </summary>
        /// <typeparam name="T">The enum type being examined.</typeparam>
        /// <param name="description">The text description we're trying to find an enum for.</param>
        /// <returns>Returns the enum matching the description, or the default enum value.</returns>
        public static T GetValueFromDescription<T>(string description)
        {
            var typeInfo = typeof(T).GetTypeInfo();

            if (!typeInfo.IsEnum)
                throw new InvalidOperationException();

            foreach (var fieldInfo in typeInfo.DeclaredFields)
            {
                EnumDescriptionAttribute fieldAttribute = fieldInfo.GetCustomAttribute<EnumDescriptionAttribute>();

                if (fieldAttribute?.Description == description || (fieldAttribute == null && fieldInfo.Name == description))
                {
                    return (T)fieldInfo.GetValue(null);
                }
            }

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
