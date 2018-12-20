﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetTally.Attributes;

namespace NetTally.Extensions
{
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
        public static T GetValueFromDescription<T>(string description) where T : struct, Enum
        {
            var typeInfo = typeof(T).GetTypeInfo();

            foreach (var fieldInfo in typeInfo.DeclaredFields)
            {
                EnumDescriptionAttribute fieldAttribute = fieldInfo.GetCustomAttribute<EnumDescriptionAttribute>();

                if (fieldAttribute?.Description == description || (fieldAttribute == null && fieldInfo.Name == description))
                {
                    return (T)fieldInfo.GetValue(null);
                }
            }

            return default;
        }

        /// <summary>
        /// Create a list of enums containing each of the enumerated values.
        /// </summary>
        /// <typeparam name="T">The enum type to create a list for.</typeparam>
        /// <returns>Returns an IEnumerable list of enum values.</returns>
        public static IEnumerable<T> EnumToList<T>() where T : struct, Enum
        {
            return Enum.GetValues(typeof(T)).OfType<T>();
        }

        /// <summary>
        /// Create a list of the descriptions of each enum value of a given type.
        /// </summary>
        /// <typeparam name="T">The enum type to create a list for.</typeparam>
        /// <returns>Returns a list of string descriptions for an enum type.</returns>
        public static IEnumerable<string> EnumDescriptionsList<T>() where T : struct, Enum
        {
            var enumDescrips = from Enum e in EnumToList<T>()
                               select e.GetDescription();

            return enumDescrips;
        }
    }
}
