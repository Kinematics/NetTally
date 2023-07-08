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
            ConfigureLogging(builder.Logging);

            return builder.Build();
        }

        /// <summary>
        /// Use the configuration builder to add user configuration files to
        /// be read from.
        /// </summary>
        /// <param name="configuration">The configuration builder.</param>
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

        /// <summary>
        /// Add binding to the types of options stored in the user configuration.
        /// </summary>
        /// <param name="services">The services collection that allows us to set up options.</param>
        private static void ConfigureOptions(IServiceCollection services)
        {
            services.AddOptions<GlobalSettings>().BindConfiguration(nameof(GlobalSettings));
            services.AddOptions<UserQuests>().BindConfiguration(nameof(UserQuests));
        }

        /// <summary>
        /// Set up dependency injection services.
        /// </summary>
        /// <param name="services">The services collection to add the services to.</param>
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

        /// <summary>
        /// Set up the logging configuration.
        /// </summary>
        /// <param name="logging">The logging builder to set up.</param>
        private static void ConfigureLogging(ILoggingBuilder logging)
        {
            logging
                .AddDebug()
                .AddFile(options =>
                {
                    options.LogDirectory = GetLoggingPath();
                    options.Periodicity = PeriodicityOptions.Daily;
                    options.RetainedFileCountLimit = 7;
                })
                .AddFilter<DebugLoggerProvider>(DebugLoggingFilter)
                .AddFilter<FileLoggerProvider>(FileLoggingFilter);
        }

        /// <summary>
        /// Load legacy XML user configuration data, to be used in migration to json config files.
        /// </summary>
        /// <returns>Returns any legacy configuration.</returns>
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

        /// <summary>
        /// Get the directory path to save logs to.
        /// </summary>
        /// <returns>Returns a path to save logs to.</returns>
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
