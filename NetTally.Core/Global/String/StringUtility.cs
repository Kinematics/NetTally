using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NetTally.Utility
{
    /// <summary>
    /// Class for general static functions relating to text manipulation and comparisons.
    /// </summary>
    public static class StringUtility
    {
        /// <summary>
        /// Magic character (currently ◈, \u25C8) to mark a named voter as a plan rather than a user.
        /// </summary>
        public static string PlanNameMarker { get; } = "◈";

        /// <summary>
        /// Check if the provided name starts with the plan name marker.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>Returns true if the name starts with the plan name marker.</returns>
        public static bool IsPlanName(this string name) => name?.StartsWith(PlanNameMarker, StringComparison.Ordinal) ?? false;

        /// <summary>
        /// Lookup table for non-latin characters that doesn't require string.Normalize (which isn't available
        /// in .NET Standard below 2.0).
        /// </summary>
        static Dictionary<string, string> nonlatin_characters = new Dictionary<string, string>
        {
            { "äæǽ", "ae" },
            { "ÆǼ", "AE" },
            { "Ä", "Ae" },
            { "öœ", "oe" },
            { "Ö", "Oe" },
            { "ü", "ue" },
            { "Ü", "Ue" },
            { "ÀÁÂÃÄÅǺĀĂĄǍΑΆẢẠẦẪẨẬẰẮẴẲẶА", "A" },
            { "àáâãåǻāăąǎªαάảạầấẫẩậằắẵẳặа", "a" },
            { "Б", "B" },
            { "б", "b" },
            { "ÇĆĈĊČ", "C" },
            { "çćĉċč", "c" },
            { "Д", "D" },
            { "д", "d" },
            { "ÐĎĐΔ", "Dj" },
            { "ðďđδ", "dj" },
            { "ÈÉÊËĒĔĖĘĚΕΈẼẺẸỀẾỄỂỆЕЭ", "E" },
            { "èéêëēĕėęěέεẽẻẹềếễểệеэ", "e" },
            { "Ф", "F" },
            { "ф", "f" },
            { "ĜĞĠĢΓГҐ", "G" },
            { "ĝğġģγгґ", "g" },
            { "ĤĦ", "H" },
            { "ĥħ", "h" },
            { "ÌÍÎÏĨĪĬǏĮİΗΉΊΙΪỈỊИЫ", "I" },
            { "ìíîïĩīĭǐįıηήίιϊỉịиыї", "i" },
            { "Ĵ", "J" },
            { "ĵ", "j" },
            { "ĶΚК", "K" },
            { "ķκк", "k" },
            { "ĹĻĽĿŁΛЛ", "L" },
            { "ĺļľŀłλл", "l" },
            { "М", "M" },
            { "м", "m" },
            { "ÑŃŅŇΝН", "N" },
            { "ñńņňŉνн", "n" },
            { "ÒÓÔÕŌŎǑŐƠØǾΟΌΩΏỎỌỒỐỖỔỘỜỚỠỞỢО", "O" },
            { "òóôõōŏǒőơøǿºοόωώỏọồốỗổộờớỡởợо", "o" },
            { "П", "P" },
            { "п", "p" },
            { "ŔŖŘΡР", "R" },
            { "ŕŗřρр", "r" },
            { "ŚŜŞȘŠΣС", "S" },
            { "śŝşșšſσςсʒ", "s" },
            { "ȚŢŤŦτТ", "T" },
            { "țţťŧт", "t" },
            { "ÙÚÛŨŪŬŮŰŲƯǓǕǗǙǛŨỦỤỪỨỮỬỰУ", "U" },
            { "ùúûũūŭůűųưǔǖǘǚǜυύϋủụừứữửựу", "u" },
            { "ÝŸŶΥΎΫỲỸỶỴЙ", "Y" },
            { "ýÿŷỳỹỷỵй", "y" },
            { "В", "V" },
            { "в", "v" },
            { "Ŵ", "W" },
            { "ŵ", "w" },
            { "ŹŻŽΖЗ", "Z" },
            { "źżžζз", "z" },
            { "ß", "ss" },
            { "Ĳ", "IJ" },
            { "ĳ", "ij" },
            { "Œ", "OE" },
            { "ƒ", "f" },
            { "ξ", "ks" },
            { "π", "p" },
            { "β", "v" },
            { "μ", "m" },
            { "ψ", "ps" },
            { "Ё", "Yo" },
            { "ё", "yo" },
            { "Є", "Ye" },
            { "є", "ye" },
            { "Ї", "Yi" },
            { "Ж", "Zh" },
            { "ж", "zh" },
            { "Х", "Kh" },
            { "х", "kh" },
            { "Ц", "Ts" },
            { "ц", "ts" },
            { "Ч", "Ch" },
            { "ч", "ch" },
            { "Ш", "Sh" },
            { "ш", "sh" },
            { "Щ", "Shch" },
            { "щ", "shch" },
            { "ЪъЬь", "" },
            { "Ю", "Yu" },
            { "ю", "yu" },
            { "Я", "Ya" },
            { "я", "ya" },
        };

        /// <summary>
        /// Return the simplified form of a character after removing diacriticals.
        /// </summary>
        /// <param name="c">The character to transform.</param>
        /// <returns>Returns the simplified latin form of a given character.</returns>
        public static string RemoveDiacritics(this char c)
        {
            try
            {
                UInt16 cb = Convert.ToUInt16(c);
                
                // Latin characters get returned as-is.
                if (cb < 128)
                    return c.ToString();

                // Spacing and combining characters are elided.
                if (cb >= 0x2b0 && cb <= 0x36f)
                    return string.Empty;

                // Other characters are checked against the above dictionary,
                // and converted to a representative latin value.
                foreach (KeyValuePair<string, string> entry in nonlatin_characters)
                {
                    if (entry.Key.IndexOf(c) != -1)
                    {
                        return entry.Value;
                    }
                }
            }
            catch (OverflowException)
            {
                // Shouldn't be possible, since chars are stored in 16-bit values.
                return string.Empty;
            }

            return c.ToString();
        }

        /// <summary>
        /// Return the simplified latin form of a string, after removing diacriticals.
        /// </summary>
        /// <param name="s">The string to transform.</param>
        /// <returns></returns>
        public static string RemoveDiacritics(this string s)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in s)
            {
                var r = c.RemoveDiacritics();
                if (!string.IsNullOrEmpty(r))
                    sb.Append(r);
            }

            return sb.ToString();
        }
    }
}
