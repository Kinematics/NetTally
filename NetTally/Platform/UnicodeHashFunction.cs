using System;
using System.Globalization;
using NetTally.Utility.Comparers;

namespace NetTally.Platform
{
    public class UnicodeHashFunction : IHash
    {
        /// <summary>
        /// Hashes the provided string using the given CompareOptions.
        /// Doing this allows all custom compare options to be applied in determining the hash value.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="info">The CompareInfo object doing the unicode-aware comparison.</param>
        /// <param name="options">The options to apply to the comparison.</param>
        /// <returns>Returns the hash code for the string.</returns>
        public int HashFunction(string str, CompareInfo info, CompareOptions options)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (string.IsNullOrEmpty(str))
                return 0;

            SortKey sortOrder = info.GetSortKey(str, options);

            int hash = GetByteArrayHash(sortOrder.KeyData);

            return hash;
        }

        /// <summary>
        /// Generates a hash code for an array of bytes.
        /// </summary>
        /// <param name="keyData">The key data.</param>
        /// <returns>Returns a hash code value.</returns>
        private static int GetByteArrayHash(byte[] keyData)
        {
            unchecked
            {
                const int p = 16777619;
                int hash = (int)2166136261;

                for (int i = 0; i < keyData.Length; i++)
                    hash = (hash ^ keyData[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }
    }
}
