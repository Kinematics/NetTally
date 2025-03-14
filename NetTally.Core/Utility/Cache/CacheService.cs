using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace NetTally.Utility.Cache;

public class CacheService(
    IMemoryCache memoryCache,
    ILogger<CacheService> logger)
{
    public void Add(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var store = Compress(value);

        var options = new MemoryCacheEntryOptions() { SlidingExpiration = TimeSpan.FromMinutes(15) };

        memoryCache.Set(key, store, options);
    }


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

    public void Clear()
    {
        if (memoryCache is MemoryCache cache)
        {
            logger.LogDebug("Clearing cache. Current count: {count}", Count);
            cache.Clear();
        }
    }

    public long Count
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
