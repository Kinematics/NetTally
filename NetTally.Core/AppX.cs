using System;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using NetTally.Cache;
using NetTally.Collections;
using NetTally.Configure;
using NetTally.Configure.Legacy;
using NetTally.Data;
using NetTally.Debugging.FileLogger;
using NetTally.Input.Forums;
using NetTally.Output;
using NetTally.Systems;
using NetTally.Systems.Config.Json;
using NetTally.Systems.Config.Xml;
using NetTally.Tally.ComponentsF.Counting;
using NetTally.Utility.Comparers;
using NetTally.ViewModels;
using NetTally.VoteCounting;
using NetTally.Web;

namespace NetTally;
public static class AppX
{
    public static IHost Host { get; private set; } = null!;
    public static IServiceProvider Services => Host.Services;

    public static void Initialize(Action<IServiceCollection>? servicesCallback)
    {
        Host = CreateHost(servicesCallback);

        _ = Services.GetRequiredService<Agnostic>();
    }

    public static void SaveConfiguration()
    {
        JsonConfiguration jsonConfiguration = Services.GetRequiredService<JsonConfiguration>();
        jsonConfiguration.SaveJsonConfiguration();
    }

    #region Hosting Setup
    /// <summary>
    /// Creates and configures the IHost for the application using the default builder.
    /// </summary>
    /// <returns>Returns an IHost that can run the application.</returns>
    private static IHost CreateHost(Action<IServiceCollection>? servicesCallback)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        // Load legacy config, if available.
        builder.Services.AddSingleton(LoadLegacyConfig() ?? new ConfigInfo());

        ConfigureConfiguration(builder.Configuration);
        ConfigureOptions(builder.Services);
        ConfigureLogging(builder.Logging);
        ConfigureServices(builder.Services);

        // Call callback to allow calling library to add its own services.
        servicesCallback?.Invoke(builder.Services);

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
            if (File.Exists(path))
            {
                try
                {
                    // Load the first valid config file.
                    configuration.AddJsonFile(path, optional: true);
                    break;
                }
                catch (InvalidDataException)
                {
                    // Invalid config file. Ignore and keep processing.
                }
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
        services.AddSingleton<PageCache>();
        services.AddSingleton<Agnostic>();
        services.AddSingleton<IHash, NormalHash>();
        services.AddSingleton<CheckForNewRelease>();
        services.AddSingleton(TimeProvider.System);

        services.AddTransient<HttpClientHandler, HttpClientHandler>();

        services.AddSingleton<TallyerF>();
        services.AddTransient<IVoteCounterF, VoteCounterF>();
        services.AddTransient<VoteCounterFactory>();
        services.AddTransient<IPageProvider, WebPageProvider>();
        services.AddTransient<Input.Forums.ReadingF.IForumReader, Input.Forums.ReadingF.ForumReader>();
        services.AddSingleton<Input.Forums.ForumAdaptersF.ForumAdapterFactory>();
        services.AddSingleton<ForumIdentifier>();

        // Fake service so that Avalonia doesn't crash on startup.
        services.AddTransient<Quest>();

        services.AddTransient<Input.Forums.ForumAdaptersF.PhpBBAdapter>();
        services.AddTransient<Input.Forums.ForumAdaptersF.VBulletin3Adapter>();
        services.AddTransient<Input.Forums.ForumAdaptersF.VBulletin4Adapter>();
        services.AddTransient<Input.Forums.ForumAdaptersF.VBulletin5Adapter>();
        services.AddTransient<Input.Forums.ForumAdaptersF.XenForo1Adapter>();
        services.AddTransient<Input.Forums.ForumAdaptersF.XenForo2Adapter>();
        services.AddTransient<Input.Forums.ForumAdaptersF.UnknownForumAdapter>();

        services.AddSingleton<ITextResultsProviderF, TallyOutputF>();

        services.AddSingleton<MainViewModel>();
        services.AddTransient<QuestOptionsViewModel>();
        services.AddTransient<GlobalOptionsViewModel>();
        services.AddTransient<ManageVotesViewModel>();
        services.AddTransient<TasksViewModel>();


        services.AddSingleton<QuestsInfo>();

        services.AddSingleton<IQuestsInfo>(x => x.GetRequiredService<QuestsInfo>());
        services.AddSingleton<IQuestsInfoMod>(x => x.GetRequiredService<QuestsInfo>());

        services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);

        services.AddTransient<JsonConfiguration>();
    }

    /// <summary>
    /// Load legacy XML user configuration data, to be used in migration to json config files.
    /// </summary>
    /// <returns>Returns any legacy configuration.</returns>
    private static ConfigInfo? LoadLegacyConfig()
    {
        if (LegacyNetTallyConfig.Load(out QuestCollection? quests, out string? currentQuest, GlobalOptionsConfig.Instance))
        {
            GlobalSettings gb = new()
            {
                DisplayMode = GlobalOptionsConfig.Instance.DisplayMode,
                DisplayPlansWithNoVotes = GlobalOptionsConfig.Instance.DisplayPlansWithNoVotes,
                DisableWebProxy = GlobalOptionsConfig.Instance.DisableWebProxy,
                GlobalSpoilers = GlobalOptionsConfig.Instance.GlobalSpoilers,
                RankVoteCounterMethod = GlobalOptionsConfig.Instance.RankVoteCounterMethod,
                AllowUsersToUpdatePlans = GlobalOptionsConfig.Instance.AllowUsersToUpdatePlans,
                TrackPostAuthorsUniquely = GlobalOptionsConfig.Instance.TrackPostAuthorsUniquely
            };

            ConfigInfo config = new([.. quests], currentQuest, gb);

            return config;
        }

        return null;
    }
    #endregion Hosting Setup

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
        if (GlobalOptionsConfig.Instance.DebugMode)
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
        if (GlobalOptionsConfig.Instance.DebugMode)
            return true;

        return logLevel >= LogLevel.Debug;
    }
    #endregion Log Filters
}
