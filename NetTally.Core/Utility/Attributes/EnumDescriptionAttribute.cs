using System;

namespace NetTally.Attributes
{
    /// <summary>
    /// Custom attribute to be used on enums, to provide user-friendly values.
    /// The standard DescriptionAttribute is not available due to framework target level,
    /// so for the attributes to be available to mobile products, a custom attribute is
    /// used instead.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class EnumDescriptionAttribute : Attribute
    {
        public string Description { get; }
        public EnumDescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}
