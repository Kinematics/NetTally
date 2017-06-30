using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using NetTally.Utility;

namespace NetTally.Platform
{
    /// <summary>
    /// Class to implement logging on the basic Windows OS.
    /// </summary>
    /// <seealso cref="NetTally.ILogger" />
    public class WindowsErrorLog : ILogger
    {
        #region ILogger interface methods
        /// <summary>
        /// Public function to log either a text message or an exception, or both.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="callingMethod">The method that made the request to log the error.</param>
        /// <returns>Returns the name of the file the log was saved in.</returns>
        public bool Log(string message, Exception exception = null, IClock clock = null, [CallerMemberName] string callingMethod = "")
        {
            try
            {
                if (clock == null)
                    clock = new SystemClock();

                LastLogLocation = GetLogFilename(clock);
                if (string.IsNullOrEmpty(LastLogLocation))
                    return false;

                string output = ComposeOutput(callingMethod, message, exception, clock);
                if (output == null)
                    return false;

                File.AppendAllText(LastLogLocation, output);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the last log location.
        /// </summary>
        /// <returns>Returns the last log location.</returns>
        public string LastLogLocation { get; private set; } = string.Empty;
        #endregion

        #region Private helper methods
        /// <summary>
        /// Generates the string that will be saved to the output log.
        /// </summary>
        /// <param name="callingMethod">The method that made the request to log the error.</param>
        /// <param name="message">Text message to output.</param>
        /// <param name="ex">The exception whose message and stack trace will be output.</param>
        /// <returns>Returns the compiled output string.</returns>
        private static string ComposeOutput(string callingMethod, string message, Exception ex, IClock clock)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                var time = clock.Now;

                sb.Append("Timestamp: ");
                sb.AppendLine($"{time.ToString("h:mm:ss tt")}");

                sb.Append("Version: ");
                sb.AppendLine(ProductInfo.Version);

                if (!string.IsNullOrEmpty(callingMethod))
                {
                    sb.Append("Called from: ");
                    sb.AppendLine(callingMethod);
                }
                sb.AppendLine();

                if (message != null)
                    sb.Append(message);

                if (ex != null)
                {
                    sb.Append($"Exception type: {ex.GetType().Name}\n");
                    sb.Append($"Message is: {ex.Message}\n\n");
                    sb.Append($"Stack Trace is:\n{ex.StackTrace}\n");

                    Exception iex = GetInnermostException(ex);

                    if (iex != null)
                    {
                        sb.Append("\r\n\r\n");
                        sb.Append($"Inner Message is: {iex.Message}\n\n");
                        sb.Append($"Stack Trace is:\n{iex.StackTrace}\n");
                    }
                }

                sb.Append("\r\n\r\n");
                sb.Append("**********************************************************\r\n\r\n");

                return sb.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the innermost exception of the provided exception.
        /// If there is no inner exception, returns null.
        /// </summary>
        /// <param name="ex">The exception to check.</param>
        /// <returns>Returns the innermost contained exception.</returns>
        private static Exception GetInnermostException(Exception ex)
        {
            if (ex == null)
                return null;

            if (ex.InnerException == null)
                return null;

            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            return ex;
        }

        /// <summary>
        /// Gets the name of a log file that can be used for output.
        /// </summary>
        /// <returns>Returns the full path of the log file.</returns>
        private static string GetLogFilename(IClock clock)
        {
            string path;

            try
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                if (Directory.Exists(path))
                {
                    path = Path.Combine(path, ProductInfo.Name);
                    Directory.CreateDirectory(path);
                }
            }
            catch
            {
                path = "";
            }

            var now = clock.Now;

            string date = $"{now.Year}-{now.Month}-{now.Day}";

            string filename = $"Log {date}.txt";

            path = Path.Combine(path, filename);

            return path;
        }

        #endregion
    }
}
