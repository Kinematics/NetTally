using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NetTally.SystemInfo;
using NetTally.Types.Enums;

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
}
