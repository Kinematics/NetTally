using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTTests
{
    public class LoadResource
    {
        public static async Task<string?> Read(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException(nameof(filename));

            FileInfo fi = new FileInfo(filename);

            byte[]? buffer = null;
            string? result = null;

            if (fi.Exists)
            {
                using (var reader = fi.OpenRead())
                {
                    buffer = new byte[reader.Length];

                    await reader.ReadAsync(buffer, 0, (int)reader.Length);
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
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException(nameof(filename));
            if (string.IsNullOrEmpty(content))
                throw new ArgumentNullException(nameof(content));

            FileInfo fi = new FileInfo(filename);

            using (var sr = fi.AppendText())
            {
                await sr.WriteAsync(content);
            }
        }
    }
}
