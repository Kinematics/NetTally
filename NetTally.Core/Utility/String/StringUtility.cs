using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NetTally.Utility
{
    /// <summary>
    /// Class for general static functions relating to text manipulation and comparisons.
    /// </summary>
    public static class Strings
    {
        #region Plan names
        /// <summary>
        /// Magic character (currently ◈, \u25C8) to mark a named voter as a plan rather than a user.
        /// </summary>
        public const string PlanNameMarker = "◈";
        public const char PlanNameMarkerChar = '◈';
        public const string NoRankMarker = "⊘";

        /// <summary>
        /// Check if the provided name starts with the plan name marker.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>Returns true if the name starts with the plan name marker.</returns>
        public static string MakePlanName(this string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            if (name.IsPlanName())
                return name;

            return $"{PlanNameMarker}{name}";
        }

        /// <summary>
        /// Check if the provided name starts with the plan name marker.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>Returns true if the name starts with the plan name marker.</returns>
        public static bool IsPlanName(this string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return (name[0] == PlanNameMarkerChar);
        }
        #endregion

        #region Safe strings
        /// <summary>
        /// Regex for control and formatting characters that we don't want to allow processing of.
        /// EG: \u200B, non-breaking space
        /// Regex is the character set of all control characters {C}, except for CR/LF.
        /// </summary>
        static Regex UnsafeCharsRegex { get; } = new Regex(@"[\p{C}-[\r\n]]");

        /// <summary>
        /// Remove unsafe UTF control characters from the provided string.
        /// Returns an empty string if given null.
        /// </summary>
        /// <param name="input">Any string.</param>
        /// <returns>Returns the input string with all unicode control characters (except cr/lf) removed.</returns>
        public static string RemoveUnsafeCharacters(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return UnsafeCharsRegex.Replace(input, "");
        }
        #endregion

        #region Line splitting
        /// <summary>
        /// Static array for use in GetStringLines.
        /// </summary>
        static readonly char[] newLines = new[] { '\r', '\n' };

        /// <summary>
        /// Takes an input string that is potentially composed of multiple text lines,
        /// and splits it up into a List of strings of one text line each.
        /// Does not generate empty lines.
        /// </summary>
        /// <param name="input">The input text.</param>
        /// <returns>The list of all string lines in the input.</returns>
        public static List<string> GetStringLines(this string input)
        {
            var result = new List<string>();

            if (!string.IsNullOrEmpty(input))
            {
                string[] split = input.Split(newLines, StringSplitOptions.RemoveEmptyEntries);
                result.AddRange(split);
            }

            return result;
        }

        /// <summary>
        /// Get the first line (pre-EOL) of a potentially multi-line string.
        /// </summary>
        /// <param name="input">The string to get the first line from.</param>
        /// <returns>Returns the first line of the provided string.</returns>
        public static string GetFirstLine(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var lines = GetStringLines(input);
            return lines.First();
        }

        #endregion

        #region Agnostic string comparison utilities
        /// <summary>
        /// Returns the first match within the enumerable list that agnostically
        /// equals the provided value.
        /// Extends the enumerable.
        /// </summary>
        /// <param name="self">The list to search.</param>
        /// <param name="value">The value to compare with.</param>
        /// <returns>Returns the item in the list that matches the value, or null.</returns>
        public static string AgnosticMatch(this IEnumerable<string> self, string value)
        {
            foreach (string item in self)
            {
                if (Agnostic.StringComparer.Equals(item, value))
                    return item;
            }

            return null;
        }

        /// <summary>
        /// Returns the first match within the enumerable list that agnostically
        /// equals the provided value.
        /// Extends a string.
        /// </summary>
        /// <param name="value">The value to compare with.</param>
        /// <param name="list">The list to search.</param>
        /// <returns>Returns the item in the list that matches the value, or null.</returns>
        public static string AgnosticMatch(this string value, IEnumerable<string> list)
        {
            foreach (string item in list)
            {
                if (Agnostic.StringComparer.Equals(item, value))
                    return item;
            }

            return null;
        }
        #endregion

        #region Diacritical cleanup
        /// <summary>
        /// Return the simplified latin form of a string, after removing diacriticals.
        /// </summary>
        /// <param name="s">The string to transform.</param>
        /// <returns></returns>
        public static string RemoveDiacritics(this string s)
        {
            StringBuilder sb = new StringBuilder();

            // Update the detailed conversion table on first use.
            if (translate_characters.Count == 0)
                TransferCharacters();

            foreach (char c in s)
            {
                sb.Append(c.RemoveDiacritics());
            }

            return sb.ToString();
        }
        #endregion

        #region Diacritic cleanup support code
        /// <summary>
        /// Lookup table for non-latin characters that doesn't require string.Normalize (which isn't available
        /// in .NET Standard below 2.0).
        /// </summary>
        static readonly Dictionary<string, string> nonlatin_characters = new Dictionary<string, string>
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
        /// Direct conversion version of the above table that splits out all individual
        /// key characters so that we can do a direct lookup instead of indexing into
        /// a string every time.
        /// Speeds worst-case comparisons up by a factor of about 10.
        /// </summary>
        static readonly Dictionary<char, string> translate_characters = new Dictionary<char, string>();

        /// <summary>
        /// Function to copy the nonlatin_characters table to the translate_characters table.
        /// </summary>
        private static void TransferCharacters()
        {
            foreach (var translation in nonlatin_characters)
            {
                foreach (char ch in translation.Key)
                {
                    translate_characters[ch] = translation.Value;
                }
            }
        }

        /// <summary>
        /// Return the simplified form of a character after removing diacriticals.
        /// </summary>
        /// <param name="c">The character to transform.</param>
        /// <returns>Returns the simplified latin form of a given character.</returns>
        private static string RemoveDiacritics(this char c)
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

                // Do a lookup check against all other characters.
                if (translate_characters.TryGetValue(c, out string normal))
                    return normal;

                // If no conversion is done, just return the original character.
                return c.ToString();
            }
            catch (OverflowException)
            {
                // Shouldn't be possible, since chars are stored in 16-bit values.
                return string.Empty;
            }
        }
        #endregion
    }
}
