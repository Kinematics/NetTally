using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NetTally.SystemInfo;

namespace NetTally
{
    /// <summary>
    /// Class for storing an ILoggerFactory that can be used to log messages
    /// from classes that aren't built using dependency injection, and have
    /// no ILogger with personal category.
    /// </summary>
    public class Logger2
    {
        #region Constructor
        static ILoggerFactory? loggerFactory;

        /// <summary>
        /// Constructor that stores the logger factory provided by the dependency injection root.
        /// Should be called early in program startup.
        /// </summary>
        /// <param name="factory">The factory used to generate a logger for static logging.</param>
        public Logger2(ILoggerFactory factory)
        {
            loggerFactory = factory;
        }
        #endregion

        #region Public static functions
        public static void LogTrace(string message,
            [CallerFilePath] string callerFilePath = "Unknown", [CallerMemberName] string callerMemberName = "Unknown")
        {
            string callerTypeName = Path.GetFileNameWithoutExtension(callerFilePath);

            loggerFactory?.CreateLogger($"{callerTypeName}:{callerMemberName}").LogTrace(message);
        }

        public static void LogDebug(string message,
            [CallerFilePath] string callerFilePath = "Unknown", [CallerMemberName] string callerMemberName = "Unknown")
        {
            string callerTypeName = Path.GetFileNameWithoutExtension(callerFilePath);

            loggerFactory?.CreateLogger($"{callerTypeName}:{callerMemberName}").LogDebug(message);
        }

        public static void LogWarning(string message,
            [CallerFilePath] string callerFilePath = "Unknown", [CallerMemberName] string callerMemberName = "Unknown")
        {
            string callerTypeName = Path.GetFileNameWithoutExtension(callerFilePath);

            loggerFactory?.CreateLogger($"{callerTypeName}:{callerMemberName}").LogWarning(message);
        }

        public static void LogWarning(Exception e, string message,
            [CallerFilePath] string callerFilePath = "Unknown", [CallerMemberName] string callerMemberName = "Unknown")
        {
            string callerTypeName = Path.GetFileNameWithoutExtension(callerFilePath);

            loggerFactory?.CreateLogger($"{callerTypeName}:{callerMemberName}").LogWarning(e, message);
        }

        public static void LogError(string message,
            [CallerFilePath] string callerFilePath = "Unknown", [CallerMemberName] string callerMemberName = "Unknown")
        {
            string callerTypeName = Path.GetFileNameWithoutExtension(callerFilePath);

            loggerFactory?.CreateLogger($"{callerTypeName}:{callerMemberName}").LogError(message);
        }

        public static void LogError(Exception e, string message,
            [CallerFilePath] string callerFilePath = "Unknown", [CallerMemberName] string callerMemberName = "Unknown")
        {
            string callerTypeName = Path.GetFileNameWithoutExtension(callerFilePath);

            loggerFactory?.CreateLogger($"{callerTypeName}:{callerMemberName}").LogError(e, message);
        }

        public static void LogCritical(string message,
            [CallerFilePath] string callerFilePath = "Unknown", [CallerMemberName] string callerMemberName = "Unknown")
        {
            string callerTypeName = Path.GetFileNameWithoutExtension(callerFilePath);

            loggerFactory?.CreateLogger($"{callerTypeName}:{callerMemberName}").LogCritical(message);
        }

        public static void LogCritical(Exception e, string message,
            [CallerFilePath] string callerFilePath = "Unknown", [CallerMemberName] string callerMemberName = "Unknown")
        {
            string callerTypeName = Path.GetFileNameWithoutExtension(callerFilePath);

            loggerFactory?.CreateLogger($"{callerTypeName}:{callerMemberName}").LogCritical(e, message);
        }
        #endregion Public static functions
    }

    #region Obsolete Logger
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
    [Obsolete("Superceded by ILogger extensions package.")]
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
    #endregion Obsolete Logger
}
