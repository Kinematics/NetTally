using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NetTally.Tests;

public class LoadResource
{
    public static async Task<string?> Read(string filename)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(filename);

        FileInfo fi = new(filename);

        byte[]? buffer = null;
        string? result = null;

        if (fi.Exists)
        {
            using (var reader = fi.OpenRead())
            {
                buffer = new byte[reader.Length];

                int amountRead = await reader.ReadAsync(buffer.AsMemory(0, (int)reader.Length));
            }

            if (buffer != null)
            {
                result = Encoding.UTF8.GetString(buffer);
            }
        }

        return result;
    }

    public static async Task Write(string filename, string content)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(filename);
        ArgumentNullException.ThrowIfNullOrEmpty(content);

        FileInfo fi = new(filename);

        using var sr = fi.AppendText();
        await sr.WriteAsync(content);
    }
}
