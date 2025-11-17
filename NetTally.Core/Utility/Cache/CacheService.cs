using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NetTally.Debugging.Logging;

namespace NetTally.Utility.Cache;

public class CacheService(
    IMemoryCache memoryCache,
    ILogger<CacheService> logger)
{
    /// <summary>
    /// Add a string to the cache.
    /// </summary>
    /// <param name="key">The lookup key for the string value. Cannot be null.</param>
    /// <param name="value">The string to be stored. Cannot be null.</param>
    public void Add(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var store = Compress(value);

        var options = new MemoryCacheEntryOptions() { SlidingExpiration = TimeSpan.FromMinutes(15) };

        memoryCache.Set(key, store, options);
    }

    /// <summary>
    /// Try to get a stored string value from the cache.
    /// </summary>
    /// <param name="key">The lookup key for the string value. Cannot be null.</param>
    /// <param name="value">The out parameter for the found string, if any.</param>
    /// <returns><c>True</c> if the requested item was found. Otherwise <c>false</c>.</returns>
    public bool TryGet(string key, [NotNullWhen(true)] out string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        if (memoryCache.TryGetValue<byte[]>(key, out var store) && store is not null)
        {
            value = Decompress(store);
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Clears the current cache.
    /// </summary>
    /// <returns><c>True</c> if the cache was successfully cleared. Otherwise <c>false</c>.</returns>
    public bool Clear()
    {
        if (memoryCache is MemoryCache cache)
        {
            logger.ClearingCache(cache.Count);
            cache.Clear();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a count of the current number of items in the cache.
    /// </summary>
    public int Count
    {
        get
        {
            if (memoryCache is MemoryCache cache)
            {
                return cache.Count;
            }

            return -1;
        }
    }

    /// <summary>
    /// Compresses the input string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>Returns the string compressed into a GZipped byte array.</returns>
    private static byte[] Compress(string input)
    {
        using MemoryStream ms = new();
        using (GZipStream zs = new(ms, CompressionMode.Compress, true))
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            zs.Write(inputBytes, 0, inputBytes.Length);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Gets the uncompressed string.
    /// </summary>
    /// <param name="input">The input byte array.</param>
    /// <returns>Returns the uncompressed string.</returns>
    private static string Decompress(byte[] input)
    {
        if (input == null)
            return string.Empty;

        using MemoryStream mso = new();
        using (MemoryStream ms = new(input))
        using (GZipStream zs = new(ms, CompressionMode.Decompress, true))
        {
            zs.CopyTo(mso);
        }

        return Encoding.UTF8.GetString(mso.ToArray());
    }
}
