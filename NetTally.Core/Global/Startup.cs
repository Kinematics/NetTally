﻿using System;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using NetTally.Cache;
using NetTally.Debugging.FileLogger;
using NetTally.Forums;
using NetTally.Options;
using NetTally.Output;
using NetTally.SystemInfo;
using NetTally.Utility.Comparers;
using NetTally.ViewModels;
using NetTally.VoteCounting;
using NetTally.VoteCounting.RankVotes;
using NetTally.Votes;
using NetTally.Web;

namespace NetTally
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
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
                .AddFilter<DebugLoggerProvider>(DebugLoggingFilter)
                .AddFilter<FileLoggerProvider>(FileLoggingFilter)
            );
            //services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);

            services.AddSingleton<IGlobalOptions>(AdvancedOptions.Instance);
            services.AddSingleton<IGeneralInputOptions>(AdvancedOptions.Instance);
            services.AddSingleton<IGeneralOutputOptions>(AdvancedOptions.Instance);

            services.AddSingleton<ViewModel>();
            services.AddSingleton<ICache<string>, PageCache>();
            services.AddSingleton<IClock, SystemClock>();
            services.AddSingleton<IHash, NormalHash>();
            services.AddSingleton<IAgnostic, Agnostic>();
            services.AddSingleton<CheckForNewRelease>();
            services.AddSingleton<Logger2>();

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
        public static string GetLoggingDirectoryPath()
        {
            try
            {
                // First check where the runtime is located.  If it's the same as the application itself,
                // this is a self-contained app, and it should use a local Logs directory.
                string runtimePath = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
                string appLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string? appPath = Path.GetDirectoryName(appLocation);

                if (appPath is not null &&
                    string.Compare(Path.GetFullPath(runtimePath).TrimEnd('\\'),
                                   Path.GetFullPath(appPath).TrimEnd('\\'),
                                   StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return "Logs";
                }

                // If we're not running a self-contained app, check for permissions to write
                // to the application data folder.
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

        public static bool FileLoggingFilter(string? category, LogLevel logLevel)
        {
            if (AdvancedOptions.Instance.DebugMode)
                return logLevel >= LogLevel.Debug;

            return logLevel >= LogLevel.Warning;
        }

        public static bool DebugLoggingFilter(string? category, LogLevel logLevel)
        {
            if (AdvancedOptions.Instance.DebugMode)
                return true;

            return logLevel >= LogLevel.Debug;
        }
    }
}
