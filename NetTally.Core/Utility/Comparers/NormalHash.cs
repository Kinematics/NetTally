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
        /// </summary>
        public int HashFunction(string str, CompareInfo info, CompareOptions options)
        {
            var normalized = str.Normalize(NormalizationForm.FormKD).AsSpan();

            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;
                ushort ch = 0;

                for (int i = 0; i < normalized.Length; i++)
                {
                    // only hash from letters
                    if (char.IsLetterOrDigit(normalized[i]))
                    {
                        // Start hash2 at i == 1
                        if (ch > 0)
                        {
                            hash2 = ((hash2 << 5) + hash2) ^ ch;
                        }

                        // convert to lowercase if it's in ASCII range
                        ch = Convert.ToUInt16(normalized[i]);
                        if (ch > 64 && ch < 91)
                            ch += 32;

                        hash1 = ((hash1 << 5) + hash1) ^ ch;
                    }
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}
