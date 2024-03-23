using System;
using System.IO;
using System.Linq;
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
using NetTally.Avalonia.Config.Json;
using NetTally.Avalonia.Config.Xml;
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


        public App()
        {
            host = CreateHost();

            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            logger = loggerFactory.CreateLogger<App>();

            // Create handlers for unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            logger.LogDebug("Application constructor completed.");
        }


        #region Hosting Setup
        /// <summary>
        /// Creates and configures the IHost for the application using the default builder.
        /// </summary>
        /// <returns>Returns an IHost that can run the application.</returns>
        private static IHost CreateHost()
        {
            var builder = Host.CreateApplicationBuilder();

            // Load legacy config, if available.
            if (LoadLegacyConfig() is ConfigInfo legacyConfig)
            {
                builder.Services.AddKeyedSingleton(ConfigStrings.LegacyKey, legacyConfig);
            }

            ConfigureConfiguration(builder.Configuration);
            ConfigureOptions(builder.Services);
            ConfigureLogging(builder.Logging);
            ConfigureServices(builder.Services);

            return builder.Build();
        }

        /// <summary>
        /// Handle setting up configuration files for the program to load configuration
        /// data from.
        /// </summary>
        /// <param name="configuration">The configuration manager of the Host.</param>
        private static void ConfigureConfiguration(ConfigurationManager configuration)
        {
            foreach (var path in JsonConfiguration.GetConfigurationPaths())
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
        /// Configure the options that can be loaded from configuration files.
        /// Bind them to defined classes.
        /// </summary>
        /// <param name="services">The services collection of the Host.</param>
        private static void ConfigureOptions(IServiceCollection services)
        {
            services.AddOptions<GlobalSettings>().BindConfiguration(nameof(GlobalSettings));
            services.AddOptions<UserQuests>().BindConfiguration(nameof(UserQuests));
        }

        /// <summary>
        /// Configure the logging details for the program to use.
        /// </summary>
        /// <param name="logging">The logging builder of the Host.</param>
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
        /// Add all the services that the Host will manage while running the application.
        /// </summary>
        /// <param name="services">The service collection of the Host.</param>
        private static void ConfigureServices(IServiceCollection services)
        {
            // Get the services provided by the core library.
            Startup.ConfigureServices(services);

            services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);

            services.AddTransient<JsonConfiguration>();

            // Then add services known by the current assembly,
            // or override services provided by the core library.

            // Add IoCNavigationService for the application.
            services.AddSingleton<Navigation.AvaloniaNavigationService>();

            // Register all the windows that the applications can display.
            services.AddTransient<Views.MainWindow>();
            services.AddTransient<Views.GlobalOptions>();
            services.AddTransient<Views.QuestOptions>();
            services.AddTransient<Views.ManageVotes>();
            services.AddTransient<Views.ReorderTasks>();
        }

        /// <summary>
        /// Load legacy XML user configuration data, to be used in migration to json config files.
        /// </summary>
        /// <returns>Returns any legacy configuration.</returns>
        private static ConfigInfo? LoadLegacyConfig()
        {
            if (LegacyNetTallyConfig.Load(out QuestCollection? quests, out string? currentQuest, AdvancedOptions.Instance))
            {
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

                ConfigInfo config = new([.. quests], currentQuest, gb);

                return config;
            }

            return null;
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

                // Add event handler for when the program exits.
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
            JsonConfiguration jsonConfiguration = host.Services.GetRequiredService<JsonConfiguration>();
            jsonConfiguration.SaveJsonConfiguration();

            logger.LogDebug("Application exit.");
        }
        #endregion Avalonia

        #region Logging
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
                    try
                    {
                        path = Path.Combine(path, ProductInfo.Name, "Logs");
                        Directory.CreateDirectory(path);

                        return path;
                    }
                    catch (Exception)
                    {
                        // If attempt to use the common app data path fails, fall back on the simple "Logs" path.
                        return "Logs";
                    }
                }
            }

            return "Logs";
        }

        /// <summary>
        /// Filter function for handling logs that get sent to the file logger.
        /// Will normally log warnings, but will log debug levels if DebugMode is on.
        /// </summary>
        /// <param name="category">The log category.</param>
        /// <param name="logLevel">The log level.</param>
        /// <returns>True if the event should be logged, or false if not.</returns>
        private static bool FileLoggingFilter(string? category, LogLevel logLevel)
        {
            if (AdvancedOptions.Instance.DebugMode)
                return logLevel >= LogLevel.Debug;

            return logLevel >= LogLevel.Warning;
        }

        /// <summary>
        /// Filter function for handling logs that get sent to the debug logger.
        /// Will normally log debug, but will log anything if DebugMode is on.
        /// </summary>
        /// <param name="category">The log category.</param>
        /// <param name="logLevel">The log level.</param>
        /// <returns>True if the event should be logged, or false if not.</returns>
        private static bool DebugLoggingFilter(string? category, LogLevel logLevel)
        {
            if (AdvancedOptions.Instance.DebugMode)
                return true;

            return logLevel >= LogLevel.Debug;
        }
        #endregion Log Filters

        #region Error Handling
        /// <summary>
        /// Special handlers if an exception isn't handled by the program.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.LogCritical((Exception)e.ExceptionObject, "Unhandled exception");
        }
        #endregion Error Handling
    }
}
