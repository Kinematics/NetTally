using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTally.Cache;
using NetTally.Forums;
using NetTally.Options;
using NetTally.Output;
using NetTally.SystemInfo;
using NetTally.Utility;
using NetTally.Utility.Comparers;
using NetTally.ViewModels;
using NetTally.VoteCounting;
using NetTally.VoteCounting.RankVotes;
using NetTally.Votes;
using NetTally.Web;
using NetTally.Debugging.FileLogger;
using System.IO;

namespace NetTally
{
    public static class Startup 
    {
        public static void ConfigureServices(IServiceCollection services, LogLevel defaultLoggingLevel = LogLevel.Debug)
        {
            // Logging system.
            services.AddLogging(builder => 
                builder
                .AddDebug()
                .AddFile(options =>
                        {
                            options.LogDirectory = GetLoggingDirectoryPath();
                            options.Periodicity = PeriodicityOptions.Daily;
                            options.RetainedFileCountLimit = 7;
                        })
            );
            services.Configure<LoggerFilterOptions>(options => options.MinLevel = defaultLoggingLevel);

            services.AddSingleton<IGlobalOptions>(AdvancedOptions.Instance);
            services.AddSingleton<IGeneralInputOptions>(AdvancedOptions.Instance);
            services.AddSingleton<IGeneralOutputOptions>(AdvancedOptions.Instance);

            services.AddSingleton<ViewModel>();
            services.AddSingleton<ViewModelService>();
            services.AddSingleton<ICache<string>, PageCache>();
            services.AddSingleton<IClock, SystemClock>();
            services.AddSingleton<IHash, NormalHash>();
            services.AddSingleton<CheckForNewRelease>();

            services.AddTransient<HttpClientHandler, HttpClientHandler>();

            services.AddSingleton<Tally>();
            services.AddSingleton<ForumAdapterFactory>();
            services.AddSingleton<IVoteCounter, VoteCounter>();
            services.AddTransient<IPageProvider, WebPageProvider>();
            services.AddTransient<ForumReader>();

            services.AddSingleton<VoteConstructor>();
            services.AddSingleton<RankVoteCounterFactory>();
            services.AddTransient<ITextResultsProvider, TallyOutput>();
        }

        /// <summary>
        /// Get a logging directory to initialize the FileLogger's options with.
        /// </summary>
        /// <returns>Returns a path to a directory to store log files in.</returns>
        private static string GetLoggingDirectoryPath()
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                if (Directory.Exists(path))
                {
                    path = Path.Combine(path, ProductInfo.Name);
                    Directory.CreateDirectory(path);

                    return path;
                }
            }
            catch (Exception)
            {
            }

            // If we don't have access to the AppData path, just fall back to a Logs subdirectory.
            return "Logs";
        }

        public static LogLevel FileLogLevel = LogLevel.Information;

        /// <summary>
        /// Supplementary check for whether logging is enabled for the specified log level.
        /// </summary>
        /// <param name="logLevel">The log level of the message being logged.</param>
        /// <returns>Returns true if logging the specified logLevel is valid. False if it shouldn't be logged.</returns>
        public static bool IsFileLoggingEnabled(LogLevel logLevel)
        {
            if (AdvancedOptions.Instance.DebugMode)
                return true;

            return logLevel >= FileLogLevel;
        }
    }
}
