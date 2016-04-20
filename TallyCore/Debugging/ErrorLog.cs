using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Runtime.CompilerServices;
using NetTally.Utility;

namespace NetTally
{
    /// <summary>
    /// Interface that can be used to supply the static ErrorLog class with a logging
    /// implementation for the platform being run.
    /// </summary>
    public interface IErrorLog
    {
        string Log(string message = null, Exception exception = null, [CallerMemberName] string callingMethod = "", IClock clock = null);
    }

    public class EmptyLogger : IErrorLog
    {
        public string Log(string message = null, Exception exception = null, [CallerMemberName] string callingMethod = "", IClock clock = null) => string.Empty;
    }

    /// <summary>
    /// Static entry point for logging error messages.
    /// </summary>
    public static class ErrorLog
    {
        /// <summary>
        /// Initializes this class to use the specified error log.
        /// </summary>
        /// <param name="errorLog">The error log.</param>
        public static void Initialize(IErrorLog errorLog)
        {
            externalLogger = errorLog;
        }

        static IErrorLog externalLogger = new EmptyLogger();

        /// <summary>
        /// Shortcut function to log an exception.
        /// </summary>
        /// <param name="e">Exception to be logged.</param>
        /// <param name="callingMethod">The method that made the request to log the error.</param>
        /// <returns>Returns the name of the file the error was logged to.</returns>
        public static string Log(Exception e, [CallerMemberName] string callingMethod = "", IClock clock = null) => Log(exception: e, callingMethod: callingMethod, clock: clock);

        /// <summary>
        /// Function to log either a text message or an exception, or both.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="callingMethod">The method that made the request to log the error.</param>
        /// <returns>Returns the name of the file the log was saved in.</returns>
        public static string Log(string message = null, Exception exception = null, [CallerMemberName] string callingMethod = "", IClock clock = null)
        {
            return externalLogger?.Log(message, exception, callingMethod, clock);
        }
    }
}
