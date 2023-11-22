using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Options;
using NetTally.Avalonia.Config;
using NetTally.Collections;
using NetTally.Debugging.FileLogger;
using NetTally.Global;
using NetTally.Options;
using NetTally.SystemInfo;

namespace NetTally.Avalonia
{
    public class App : Application
    {
        private readonly IHost host;
        private readonly ILogger<App> logger;
        public IServiceProvider Services => host.Services;

        const string UserConfigJsonFile = "userconfig.json";


        public App()
        {
            host = CreateHost();

            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            logger = loggerFactory.CreateLogger<App>();

            // Create handlers for unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            logger.LogInformation("App constructor completed.");
        }


        #region Hosting Setup
        private IHost CreateHost()
        {
            var builder = Host.CreateApplicationBuilder();

            ConfigureConfiguration(builder.Configuration);
            ConfigureOptions(builder.Services);
            ConfigureLogging(builder.Logging);
            ConfigureServices(builder.Services);

            return builder.Build();
        }

        private static void ConfigureConfiguration(ConfigurationManager configuration)
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

        private static void ConfigureServices(IServiceCollection services)
        {
            // Get the services provided by the core library.
            Startup.ConfigureServices(services);

            services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);

            // Then add services known by the current assembly,
            // or override services provided by the core library.

            // Add IoCNavigationService for the application.
            services.AddSingleton<Navigation.AvaloniaNavigationService>();

            // Register all the Windows of the applications via the service provider.
            services.AddTransient<Views.MainWindow>();
            services.AddTransient<Views.GlobalOptions>();
            services.AddTransient<Views.QuestOptions>();
            services.AddTransient<Views.ManageVotes>();
            //            services.AddTransient<ReorderTasksWindow>();
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

            ConfigInfo config = new(quests.ToList(), currentQuest, gb);

            return config;
        }
        #endregion Hosting Setup

        #region Avalonia
        /// <summary>
        /// Initializes the Application Framework, loading resources and the like from the XAML.
        /// </summary>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Initialize the Comparers
            Utility.Comparers.Agnostic.Init(Services.GetRequiredService<Utility.Comparers.IHash>());

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                BindingPlugins.DataValidators.RemoveAt(0);

                desktop.Exit += Desktop_Exit;

                Views.MainWindow mainWindow = Services.GetRequiredService<Views.MainWindow>();

                desktop.MainWindow = mainWindow;
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            // Save settings on exit.
            SaveJsonConfiguration();

            logger.LogDebug("Application exit.");
        }
        #endregion Avalonia


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
