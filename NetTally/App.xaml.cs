/*
    NetTally
    Copyright(C) 2015  David Smith <dsmith@datasync.com>

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Options;
using NetTally.Collections;
using NetTally.Debugging.FileLogger;
using NetTally.Global;
using NetTally.Navigation;
using NetTally.Options;
using NetTally.SystemInfo;
using NetTally.Utility.Comparers;
using NetTally.ViewModels;
using NetTally.Views;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost host;
        private readonly ILogger<App> logger;

        const string UserConfigJsonFile = "userconfig.json";

        public App()
        {
            // Create handlers for unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Create host.
            host = CreateHost();

            // Create logger for the app.
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            logger = loggerFactory.CreateLogger<App>();

            //var oldhost = Host.CreateDefaultBuilder(Environment.GetCommandLineArgs())
            //    .ConfigureAppConfiguration(ConfigureConfiguration1)
            //    .ConfigureServices(ConfigureServices)
            //    .ConfigureLogging(ConfigureLogging)
            //    .Build();
        }

        #region Startup and Shutdown
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                // Start the app
                await host.StartAsync();

                InitializeStartup();

                logger.LogInformation("Starting application. Version: {version}", ProductInfo.Version);

                // Request the navigation service and create our main window.
                var navigationService = host.Services.GetRequiredService<IoCNavigationService>();
                await navigationService.ShowAsync<MainWindow2>();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error during application startup");
            }
        }

        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            using (host)
            {
                // Save user config
                SaveJsonConfiguration();

                // Wait up to 5 seconds before forcing a shutdown.
                await host.StopAsync(TimeSpan.FromSeconds(5));
            }
        }

        private void InitializeStartup()
        {
            // Initialize string comparer system.
            var hash = host.Services.GetRequiredService<IHash>();
            Agnostic.Init(hash);
        }
        #endregion Startup and Shutdown

        #region Setup
        private static IHost CreateHost()
        {
            var builder = Host.CreateApplicationBuilder();

            // Load legacy config
            ConfigInfo legacyConfig = LoadLegacyConfig();
            builder.Services.AddSingleton(legacyConfig);

            ConfigureConfiguration(builder.Configuration);
            ConfigureOptions(builder.Services);
            ConfigureServices(builder.Services);
            ConfigureLogging(builder.Environment, builder.Logging);

            return builder.Build();
        }

        private static void ConfigureConfiguration(IConfigurationBuilder configuration)
        {
            // Add additional files for the configuration manager to load options from.
            foreach (var path in GetConfigurationPaths())
            {
                try
                {
                    configuration.AddJsonFile(path, optional: true);
                }
                catch (InvalidDataException)
                {
                    // Invalid config file. Ignore and keep processing.
                }
            }
        }

        private static void ConfigureOptions(IServiceCollection services)
        {
            services.AddOptions<GlobalSettings>().BindConfiguration(nameof(GlobalSettings));
            services.AddOptions<UserQuests>().BindConfiguration(nameof(UserQuests));
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Get the services provided by the core library.
            NetTally.Startup.ConfigureServices(services);

            // Add IoCNavigationService for the application.
            services.AddSingleton<IoCNavigationService>();

            // Register all the Windows of the applications.
            services.AddTransient<MainWindow>();
            services.AddTransient<MainWindow2>();
            services.AddTransient<GlobalOptions>();
            services.AddTransient<GlobalOptions2>();
            services.AddTransient<QuestOptions>();
            services.AddTransient<QuestOptions2>();
            services.AddTransient<ManageVotes>();
            services.AddTransient<ManageVotes2>();
            services.AddTransient<ReorderTasks>();
            services.AddTransient<ReorderTasks2>();
        }

        private static void ConfigureLogging(IHostEnvironment environment, ILoggingBuilder logging)
        {
            logging
                .AddDebug()
                .AddFile(options =>
                {
                    options.LogDirectory = GetLoggingDirectoryPath();
                    options.Periodicity = PeriodicityOptions.Daily;
                    options.RetainedFileCountLimit = 7;
                })
                .AddFilter<DebugLoggerProvider>(DebugLoggingFilter)
                .AddFilter<FileLoggerProvider>(FileLoggingFilter);
        }

        private static ConfigInfo LoadLegacyConfig()
        {
            NetTallyConfig.Load(out QuestCollection quests, out string? currentQuest, AdvancedOptions.Instance);

            GlobalSettings gb = new()
            {
                DisplayMode = AdvancedOptions.Instance.DisplayMode,
                DisplayPlansWithNoVotes = AdvancedOptions.Instance.DisplayPlansWithNoVotes,
                DisableWebProxy = AdvancedOptions.Instance.DisableWebProxy,
                GlobalSpoilers = AdvancedOptions.Instance.GlobalSpoilers,
                RankVoteCounterMethod = AdvancedOptions.Instance.RankVoteCounterMethod,
                AllowUsersToUpdatePlans = AdvancedOptions.Instance.AllowUsersToUpdatePlans,
                TrackPostAuthorsUniquely = AdvancedOptions.Instance.TrackPostAuthorsUniquely
            };

            ConfigInfo config = new(quests.Select(q => (Quest)q).ToList(), currentQuest, gb);

            return config;
        }
        #endregion Setup

        #region Services
        private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            // Get the services provided by the core library.
            NetTally.Startup.ConfigureServices(services);

            // Set configuration options
            services.Configure<GlobalSettings>(context.Configuration.GetSection(nameof(GlobalSettings)));
            services.Configure<UserQuests>(context.Configuration.GetSection(nameof(UserQuests)));

            if (LegacyConfig is not null)
                services.AddSingleton(LegacyConfig);

            // Add IoCNavigationService for the application.
            services.AddSingleton<IoCNavigationService>();

            // Register all the Windows of the applications via the service provider.
            services.AddTransient<MainWindow>();
            services.AddTransient<MainWindow2>();
            services.AddTransient<GlobalOptions>();
            services.AddTransient<GlobalOptions2>();
            services.AddTransient<QuestOptions>();
            services.AddTransient<QuestOptions2>();
            services.AddTransient<ManageVotes>();
            services.AddTransient<ManageVotes2>();
            services.AddTransient<ReorderTasks>();
            services.AddTransient<ReorderTasks2>();
        }

        ConfigInfo? LegacyConfig;

        /// <summary>
        /// Add user configuration files to the configuration builder.
        /// </summary>
        /// <param name="builder"></param>
        private void ConfigureConfiguration1(IConfigurationBuilder builder)
        {
            foreach (var path in GetConfigurationPaths())
            {
                builder.AddJsonFile(path, optional: true);
            }

            // Load Legacy Config
            LegacyConfig = LoadLegacyConfig();
        }
        #endregion Services

        #region Save Configuration
        /// <summary>
        /// Saves the user configuration information to the user config file(s).
        /// </summary>
        private void SaveJsonConfiguration()
        {
            JsonSerializerOptions jsonOptions = new()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,
                IgnoreReadOnlyProperties = true
            };

            try
            {
                var config = GetConfigurationToSave();

                if (config is null)
                {
                    logger.LogWarning("Unable to save configuration. No config available.");
                    return;
                }

                foreach (var path in GetConfigurationPaths())
                {
                    using var stream = File.Create(path);

                    // Async can fail on large saves when exiting. Use sync.
                    JsonSerializer.Serialize(stream, config, jsonOptions);

                    logger.LogInformation("Configuration saved to {path}", path);
                }
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Unable to save configuration.");
            }
        }

        /// <summary>
        /// Get configuration info to save into the JSON config file.
        /// </summary>
        /// <returns>Returns current config info.</returns>
        private ConfigInfo? GetConfigurationToSave()
        {
            IQuestsInfo questsInfo = host.Services.GetRequiredService<IQuestsInfo>();
            IOptions<GlobalSettings> globalSettings = host.Services.GetRequiredService<IOptions<GlobalSettings>>();

            ConfigInfo config = new(questsInfo.Quests, questsInfo.SelectedQuest?.ThreadName, globalSettings.Value);

            return config;
        }
        #endregion Save Configuration


        #region Paths
        /// <summary>
        /// Get the available paths to load or save user configuration.
        /// This may vary depending on OS and directory permissions.
        /// </summary>
        /// <returns>Returns an enumeration of configuration file paths.</returns>
        private static IEnumerable<string> GetConfigurationPaths()
        {
            // Try to find the AppSettings path on Windows, and use it
            // first when trying to load or save user config info.
            if (OperatingSystem.IsWindows())
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                if (Path.Exists(path))
                {
                    path = Path.Combine(path, ProductInfo.Name);
                    Directory.CreateDirectory(path);

                    yield return Path.Combine(path, UserConfigJsonFile);
                }
            }

            // After that, supply the file for the local directory.
            // This will take precedence over the AppSettings version of the file, if it exists.
            yield return UserConfigJsonFile;
        }

        private static string GetLoggingPath()
        {
            if (OperatingSystem.IsWindows())
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                if (Directory.Exists(path))
                {
                    path = Path.Combine(path, ProductInfo.Name, "Logs");
                    Directory.CreateDirectory(path);

                    return path;
                }
            }

            return "Logs";
        }

        #endregion Paths

        #region Logging
        private void ConfigureLogging(HostBuilderContext context, ILoggingBuilder builder)
        {
            builder
                .AddDebug()
                .AddFile(options =>
                {
                    options.LogDirectory = GetLoggingDirectoryPath();
                    options.Periodicity = PeriodicityOptions.Daily;
                    options.RetainedFileCountLimit = 7;
                })
                .AddFilter<DebugLoggerProvider>(DebugLoggingFilter)
                .AddFilter<FileLoggerProvider>(FileLoggingFilter);
        }

        /// <summary>
        /// Get a logging directory to save file logs to.
        /// </summary>
        /// <returns>Returns a path to a directory to store log files in.</returns>
        private static string GetLoggingDirectoryPath()
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
                // to the application data folder. Windows-only.
                if (OperatingSystem.IsWindows())
                {
                    string loggingPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                    if (Directory.Exists(loggingPath))
                    {
                        loggingPath = Path.Combine(loggingPath, ProductInfo.Name);
                        Directory.CreateDirectory(loggingPath);

                        return loggingPath;
                    }
                }
            }
            catch (Exception)
            {
            }

            // If we don't have access to the AppData path, just fall back to a Logs subdirectory.
            return "Logs";
        }
        #endregion Logging

        #region Log Filters
        private static bool FileLoggingFilter(string? category, LogLevel logLevel)
        {
            if (AdvancedOptions.Instance.DebugMode)
                return logLevel >= LogLevel.Debug;

            return logLevel >= LogLevel.Warning;
        }

        private static bool DebugLoggingFilter(string? category, LogLevel logLevel)
        {
            if (AdvancedOptions.Instance.DebugMode)
                return true;

            return logLevel >= LogLevel.Debug;
        }
        #endregion Log Filters

        #region Error Handling
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.LogCritical((Exception)e.ExceptionObject, "Unhandled exception");
        }
        #endregion Error Handling
    }
}
