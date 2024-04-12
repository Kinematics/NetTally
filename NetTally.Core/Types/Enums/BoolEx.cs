using System.ComponentModel;

namespace NetTally.Types.Enums
{
    /// <summary>
    /// Enum to extend bool to include an unknown state.
    /// </summary>
    public enum BoolEx
    {
        Unknown = -1,
        False = 0,
        True = 1
    }

    public static class BoolExConverter
    {
        public static bool? Convert(BoolEx value)
        {
            return value switch
            {
                BoolEx.True => true,
                BoolEx.False => false,
                _ => null
            };
        }

        public static BoolEx Convert(bool? value)
        {
            return value switch
            {
                true => BoolEx.True,
                false => BoolEx.False,
                _ => BoolEx.Unknown
            };
        }
    }
}
