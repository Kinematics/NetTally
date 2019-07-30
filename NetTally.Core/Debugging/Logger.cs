using System;
using System.Runtime.CompilerServices;
using NetTally.SystemInfo;

namespace NetTally
{
    /// <summary>
    /// Interface that can be used to supply the static Logger class with an
    /// implementation for the platform being run.
    /// </summary>
    public interface INTLogger
    {
        bool Log(string message, IClock clock, [CallerMemberName] string callingMethod = "Unknown");
        bool Log(string message, Exception exception, IClock clock, [CallerMemberName] string callingMethod = "Unknown");
        string LastLogLocation { get; }
    }

    /// <summary>
    /// Default version of the ILogger so that failure to initialize the error logger class
    /// won't cause things to crash.
    /// </summary>
    /// <seealso cref="NetTally.INTLogger" />
    public class NullLogger : INTLogger
    {
        public bool Log(string message, IClock clock, [CallerMemberName] string callingMethod = "Unknown") => true;
        public bool Log(string message, Exception exception, IClock clock, [CallerMemberName] string callingMethod = "Unknown") => true;
        public string LastLogLocation => "Nowhere";
    }

    /// <summary>
    /// Static entry point for logging messages.
    /// </summary>
    public static class Logger
    {
        static INTLogger _logger = new NullLogger();
        public static IClock Clock { get; set; } = new SystemClock();
        public static LoggingLevel LoggingLevel { get; set; } = LoggingLevel.Error;

        public const string UnknownLogLocation = "Unknown";

        /// <summary>
        /// Cause the static Logger class to use the specified ILogger and IClock implementations.
        /// </summary>
        /// <param name="logger">The logger implementation to use.</param>
        public static void LogUsing(INTLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Log an informational message.
        /// Will only be logged if logging level is Info.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="callingMethod">The method that made the request to log the message.</param>
        /// <returns>Returns true if the message was logged. Otherwise, false.</returns>
        public static bool Info(string message, [CallerMemberName] string callingMethod = "Unknown")
        {
            if (string.IsNullOrEmpty(message))
                return false;

            if (LoggingLevel == LoggingLevel.Info)
            {
                return _logger.Log(message, Clock, callingMethod);
            }
            return false;
        }

        /// <summary>
        /// Log an informational message.
        /// Will only be logged if logging level is Info.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">An exception to go with the log.</param>
        /// <param name="callingMethod">The method that made the request to log the message.</param>
        /// <returns>Returns true if the message was logged. Otherwise, false.</returns>
        public static bool Info(string message, Exception exception, [CallerMemberName] string callingMethod = "Unknown")
        {
            if (string.IsNullOrEmpty(message))
                return false;

            if (LoggingLevel == LoggingLevel.Info)
            {
                return _logger.Log(message, exception, Clock, callingMethod);
            }
            return false;
        }

        /// <summary>
        /// Log a warning message.
        /// Will only be logged if logging level is Info or Warning.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="callingMethod">The method that made the request to log the message.</param>
        /// <returns>Returns true if the message was logged. Otherwise, false.</returns>
        public static bool Warning(string message, [CallerMemberName] string callingMethod = "Unknown")
        {
            if (string.IsNullOrEmpty(message))
                return false;

            if (LoggingLevel == LoggingLevel.Warning || LoggingLevel == LoggingLevel.Info)
            {
                return _logger.Log(message, Clock, callingMethod);
            }
            return false;
        }

        /// <summary>
        /// Log a warning message.
        /// Will only be logged if logging level is Info or Warning.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to add to the log. Optional.</param>
        /// <param name="callingMethod">The method that made the request to log the message.</param>
        /// <returns>Returns true if the message was logged. Otherwise, false.</returns>
        public static bool Warning(string message, Exception exception, [CallerMemberName] string callingMethod = "Unknown")
        {
            if (string.IsNullOrEmpty(message))
                return false;

            if (LoggingLevel == LoggingLevel.Warning || LoggingLevel == LoggingLevel.Info)
            {
                return _logger.Log(message, exception, Clock, callingMethod);
            }
            return false;
        }

        /// <summary>
        /// Log an error message.
        /// Will be logged unless the logging level is None.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to add to the log. Optional.</param>
        /// <param name="callingMethod">The method that made the request to log the message.</param>
        /// <returns>Returns true if the message was logged. Otherwise, false.</returns>
        public static bool Error(string message, [CallerMemberName] string callingMethod = "Unknown")
        {
            if (string.IsNullOrEmpty(message))
                return false;

            if (LoggingLevel != LoggingLevel.None)
            {
                return _logger.Log(message, Clock, callingMethod);
            }
            return false;
        }

        /// <summary>
        /// Log an error message.
        /// Will be logged unless the logging level is None.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to add to the log. Optional.</param>
        /// <param name="callingMethod">The method that made the request to log the message.</param>
        /// <returns>Returns true if the message was logged. Otherwise, false.</returns>
        public static bool Error(string message, Exception exception, [CallerMemberName] string callingMethod = "Unknown")
        {
            if (string.IsNullOrEmpty(message))
                return false;

            if (LoggingLevel != LoggingLevel.None)
            {
                return _logger.Log(message, exception, Clock, callingMethod);
            }
            return false;
        }

        /// <summary>
        /// Get the last location an error was logged to.
        /// </summary>
        /// <returns>Returns the last location an error was logged to.</returns>
        public static string LastLogLocation => _logger.LastLogLocation ?? UnknownLogLocation;
    }
}
