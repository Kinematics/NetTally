using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace NetTally.Utility.Cache;

public class CacheService(
    IMemoryCache memoryCache,
    ILogger<CacheService> logger)
{
    CancellationTokenSource resetCacheToken = new();

    public void Add(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var store = Compress(value);

        var options = new MemoryCacheEntryOptions() { SlidingExpiration = TimeSpan.FromMinutes(15) };
        options.AddExpirationToken(new CancellationChangeToken(resetCacheToken.Token));

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
        if (resetCacheToken != null &&
            !resetCacheToken.IsCancellationRequested &&
            resetCacheToken.Token.CanBeCanceled)
        {
            resetCacheToken.Cancel();
            resetCacheToken.Dispose();

            resetCacheToken = new CancellationTokenSource();

            var stats = memoryCache.GetCurrentStatistics();
            logger.LogDebug("Token reset and cache cleared. Current count: {count}",
                stats?.CurrentEntryCount ?? -1);
        }
    }

    public long Count
    {
        get
        {
            var stats = memoryCache.GetCurrentStatistics();
            return stats?.CurrentEntryCount ?? -1;
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
