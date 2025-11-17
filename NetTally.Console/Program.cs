using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTally.Debugging.Logging;
using NetTally.Models;
using NetTally.Product;
using NetTally.Utility.Events;
using NetTally.ViewModels;

namespace NetTally.CLI;

class Program
{
    #region Variables
    static bool verbose;

    static ILogger<Program> logger;
    static MainViewModel mainViewModel;
    static GlobalOptionsViewModel globalOptionsViewModel;
    #endregion

    #region Main entry point
    /// <summary>
    /// Main entry point for the console application.
    /// </summary>
    /// <param name="args">Arguments passed to the application.</param>
    static async Task Main(string[] args)
    {
        AppX.Initialize(_ => { });

        // Get a logger for debugging.
        var loggerFactory = AppX.Services.GetRequiredService<ILoggerFactory>();
        logger = loggerFactory.CreateLogger<Program>();

        mainViewModel = AppX.Services.GetRequiredService<MainViewModel>();
        globalOptionsViewModel = AppX.Services.GetService<GlobalOptionsViewModel>();

        mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;

        var arguments = Parser.Default.ParseArguments<Options>(args);

        logger.AppStartup(ProductInfo.Version);

        await arguments.WithParsedAsync(RunWithOptions);
    }
    #endregion

    #region Configure and Run
    /// <summary>
    /// Set things up to run the tally using the options provided.
    /// </summary>
    /// <param name="options">The options that were parsed from the commandline arguments.</param>
    public static async Task RunWithOptions(Options options)
    {
        verbose = options.Verbose;

        SetGlobalOptions(options);

        Quest quest = GetQuestWithOptions(options);

        await RunTally(quest);

    }

    /// <summary>
    /// The code to actually call the ViewModel API to run the tally.
    /// </summary>
    /// <param name="quest">The quest being tallied.</param>
    private static async Task RunTally(Quest quest)
    {
        Thread.Sleep(30);

        bool canAddQuest = mainViewModel.AddQuestCommand.CanExecute(null);

        if (canAddQuest)
        {
            mainViewModel.AddQuestCommand.Execute(quest);

            mainViewModel.SelectedQuest = quest;

            bool canRunTally = mainViewModel.RunTallyCommand.CanExecute(null);

            if (canRunTally)
            {
                await mainViewModel.RunTallyCommand.ExecuteAsync(default);
            }
        }
    }

    /// <summary>
    /// Sets the global service options on the main view model.
    /// </summary>
    /// <param name="options">The commandline options set when run.</param>
    private static void SetGlobalOptions(Options options)
    {
        globalOptionsViewModel.DisplayMode = options.DisplayMode;

        globalOptionsViewModel.GlobalSpoilers = options.SpoilerAll;

        globalOptionsViewModel.DisplayPlansWithNoVotes = options.Display0Votes;

        globalOptionsViewModel.DisableWebProxy = options.DisableWebProxy;

        globalOptionsViewModel.DebugMode = options.Debug;
    }

    /// <summary>
    /// Gets a quest instance, set according to the provided commandline options.
    /// </summary>
    /// <param name="options">Custom options to set on the quest.</param>
    /// <returns>Returns a completed Quest instance.</returns>
    private static Quest GetQuestWithOptions(Options options)
    {
        Quest quest = new()
        {
            ThreadName = options.Thread,
            PartitionMode = options.PartitionMode,
            WhitespaceAndPunctuationIsSignificant = options.Whitespace,
            CaseIsSignificant = options.Case,
            ForcePlanReferencesToBeLabeled = options.MustLabelPlanReferences,
            ForbidVoteLabelPlanNames = options.ForbidPlanLabels,
            AllowUsersToUpdatePlans = options.AllowUsersToUpdatePlans,
            DisableProxyVotes = options.NoUserProxy,
            ForcePinnedProxyVotes = options.ForcePinProxy,
            IgnoreSpoilers = options.IgnoreSpoilers,
            TrimExtendedText = options.Trim
        };

        if (options.StartPost.HasValue)
            quest.StartPost = options.StartPost.Value;
        if (options.EndPost.HasValue)
            quest.EndPost = options.EndPost.Value;

        quest.CheckForLastThreadmark = !options.StartPost.HasValue && !options.EndPost.HasValue;

        if (!string.IsNullOrEmpty(options.ThreadmarkFilters))
        {
            quest.UseCustomThreadmarkFilters = true;
            quest.CustomThreadmarkFilters = options.ThreadmarkFilters;
        }

        if (!string.IsNullOrEmpty(options.UsernameFilters))
        {
            quest.UseCustomUsernameFilters = true;
            quest.CustomUsernameFilters = options.UsernameFilters;
        }

        if (!string.IsNullOrEmpty(options.PostFilters))
        {
            quest.UseCustomPostFilters = true;
            quest.CustomPostFilters = options.PostFilters;
        }

        if (!string.IsNullOrEmpty(options.TaskFilters))
        {
            quest.UseCustomTaskFilters = true;
            quest.CustomTaskFilters = options.TaskFilters;
        }

        return quest;
    }
    #endregion


    #region Event watching
    /// <summary>
    /// Event watcher for property notification, allowing us to output the results of the tally.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private static void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e is PropertyDataChangedEventArgs<string> eData)
        {
            if (mainViewModel.IsTallyRunning && verbose)
            {
                Console.Error.Write(eData.PropertyData);
            }
        }
        else if (mainViewModel.IsTallyRunning == false)
        {
            if (e != null && e.PropertyName == nameof(mainViewModel.Output))
            {
                Console.WriteLine(mainViewModel.Output);

                if (verbose)
                    Console.Error.WriteLine("Tally completed!");
            }
        }
    }
    #endregion
}