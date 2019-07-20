using System;
using System.Globalization;
using System.Text;

namespace NetTally.Utility.Comparers
{
    public class NormalHash : IHash
    {
        /// <summary>
        /// Craft a hash function which returns identical values for strings that
        /// may vary by case or punctuation or diacriticals or punctuation.
        /// This ensures strings which the agnostic comparer may consider the same
        /// get the same hash code, so that the full comparison is actually made.
        /// Based on: https://github.com/microsoft/referencesource/blob/e0bf122d0e52a42688b92bb4be2cfd66ca3c2f07/System.Web/Util/StringUtil.cs#L257
        /// </summary>
        public int HashFunction(string str, CompareInfo info, CompareOptions options)
        {
            ReadOnlySpan<char> normalized = str.Normalize(NormalizationForm.FormKD).AsSpan();

            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;
                char c;
                bool alternate = false;

                for (int i = 0; i < normalized.Length; i++)
                {
                    // Only hash from letters and numbers. Skip spaces and punctuation.
                    if (char.IsLetterOrDigit(normalized[i]))
                    {
                        // Convert to lowercase.  Comparison is case-insensitive.
                        c = char.ToLower(normalized[i], CultureInfo.InvariantCulture);

                        // Convert to numeric value.
                        ushort ch = Convert.ToUInt16(c);

                        // Alternate between which hash we modify as we move through the characters.
                        alternate = !alternate;

                        if (alternate)
                            hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ ch;
                        else
                            hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ ch;
                    }
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}
