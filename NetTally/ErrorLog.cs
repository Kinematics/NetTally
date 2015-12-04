using System;
using System.IO;
using System.Text;
using System.Runtime.CompilerServices;

namespace NetTally
{
    /// <summary>
    /// Class to allow logging of error messages
    /// </summary>
    public static class ErrorLog
    {
        /// <summary>
        /// Public shortcut function to log an exception.
        /// </summary>
        /// <param name="e">Exception to be logged.</param>
        /// <param name="callingMethod">The method that made the request to log the error.</param>
        /// <returns>Returns the name of the file the error was logged to.</returns>
        public static string Log(Exception e, [CallerMemberName] string callingMethod = "") => Log(exception: e, callingMethod: callingMethod);

        /// <summary>
        /// Public function to log either a text message or an exception, or both.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="callingMethod">The method that made the request to log the error.</param>
        /// <returns>Returns the name of the file the log was saved in.</returns>
        public static string Log(string message = null, Exception exception = null, [CallerMemberName] string callingMethod = "")
        {
            try
            {
                string filename = GetLogFilename();
                if (filename == null || filename == string.Empty)
                    return null;

                string output = ComposeOutput(callingMethod, message, exception);
                if (output == null)
                    return null;

                File.AppendAllText(filename, output);

                return filename;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Generates the string that will be saved to the output log.
        /// </summary>
        /// <param name="callingMethod">The method that made the request to log the error.</param>
        /// <param name="message">Text message to output.</param>
        /// <param name="ex">The exception whose message and stack trace will be output.</param>
        /// <returns>Returns the compiled output string.</returns>
        private static string ComposeOutput(string callingMethod, string message, Exception ex)
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("Timestamp: ");
                sb.AppendLine(DateTime.Now.ToLongTimeString());
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
                    if (sb.Length > 0)
                        sb.Append("\r\n\r\n");

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
        private static string GetLogFilename()
        {
            string path;

            try
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                if (Directory.Exists(path))
                {
                    path = Path.Combine(path, "NetTally");
                    Directory.CreateDirectory(path);
                }
            }
            catch
            {
                path = "";
            }

            var now = DateTime.Now;

            string date = $"{now.Year}-{now.Month}-{now.Day}";

            string filename = $"Log {date}.txt";

            path = Path.Combine(path, filename);

            return path;
        }
    }
}
