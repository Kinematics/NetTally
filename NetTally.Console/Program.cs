using System;
using System.ComponentModel;
using System.Threading;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTally.CustomEventArgs;
using NetTally.ViewModels;

namespace NetTally.CLI
{
    class Program
    {
        #region Variables
        static bool verbose;

        static IServiceProvider serviceProvider;
        static ILogger<Program> logger;
        #endregion

        #region Main entry point
        /// <summary>
        /// Main entry point for the console application.
        /// </summary>
        /// <param name="args">Arguments passed to the application.</param>
        static void Main(string[] args)
        {
            // Create a service collection and configure our dependencies
            var serviceCollection = new ServiceCollection();

            // Get the services provided by the core library.
            Startup.ConfigureServices(serviceCollection, LogLevel.Trace);

            // Build the IServiceProvider and set our reference to it
            serviceProvider = serviceCollection.BuildServiceProvider();

            // Get a logger for debugging.
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            logger = loggerFactory.CreateLogger<Program>();
            logger.LogDebug("Services defined, starting console app!");

            serviceProvider.GetRequiredService<ViewModelService>();

            ViewModelService.MainViewModel.PropertyChanged += MainViewModel_PropertyChanged;
            logger.LogTrace("Watching events from the main view model.");

            var arguments = Parser.Default.ParseArguments<Options>(args);
            logger.LogTrace("Options parsed.");

            arguments.WithParsed(o => RunWithOptions(o));
        }
        #endregion

        #region Configure and Run
        /// <summary>
        /// Set things up to run the tally using the options provided.
        /// </summary>
        /// <param name="options">The options that were parsed from the commandline arguments.</param>
        public static void RunWithOptions(Options options)
        {
            logger.LogTrace("Entered RunWithOptions");

            verbose = options.Verbose;

            SetGlobalOptions(options);

            Quest quest = GetQuestWithOptions(options);

            logger.LogTrace("Options set");

            RunTally(quest);

        }

        /// <summary>
        /// The code to actually call the ViewModel API to run the tally.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        private static void RunTally(Quest quest)
        {
            Thread.Sleep(30);

            bool canAddQuest = ViewModelService.MainViewModel.AddQuestCommand.CanExecute(null);
            logger.LogTrace($"Can Add Quest: {canAddQuest} (TallyIsRunning: {ViewModelService.MainViewModel.TallyIsRunning})");

            if (canAddQuest)
            {
                ViewModelService.MainViewModel.AddQuestCommand.Execute(quest);

                logger.LogTrace("Quest added");

                ViewModelService.MainViewModel.SelectedQuest = quest;

                logger.LogTrace("Quest selected");

                bool canRunTally = ViewModelService.MainViewModel.RunTallyCommand.CanExecute(null);
                logger.LogTrace($"Can Run Tally: {canRunTally}");

                if (canRunTally)
                {
                    logger.LogTrace("Running Tally...");
                    ViewModelService.MainViewModel.DoRunTallyAsync(null).Wait();
                }
            }
        }

        /// <summary>
        /// Sets the global service options on the main view model.
        /// </summary>
        /// <param name="options">The commandline options set when run.</param>
        private static void SetGlobalOptions(Options options)
        {
            logger.LogTrace("Setting global options");

            ViewModelService.MainViewModel.Options.DisplayMode = options.DisplayMode;

            ViewModelService.MainViewModel.Options.GlobalSpoilers = options.SpoilerAll;

            ViewModelService.MainViewModel.Options.DisplayPlansWithNoVotes = options.Display0Votes;

            ViewModelService.MainViewModel.Options.DisableWebProxy = options.DisableWebProxy;

            ViewModelService.MainViewModel.Options.DebugMode = options.Debug;
        }

        /// <summary>
        /// Gets a quest instance, set according to the provided commandline options.
        /// </summary>
        /// <param name="options">Custom options to set on the quest.</param>
        /// <returns>Returns a completed Quest instance.</returns>
        private static Quest GetQuestWithOptions(Options options)
        {
            logger.LogTrace("Setting up quest with options.");

            Quest quest = new Quest()
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
            logger.LogTrace($"MainViewModel property changed event: {e.PropertyName}");

            if (e is PropertyDataChangedEventArgs<string> eData)
            {
                if (ViewModelService.MainViewModel.TallyIsRunning && verbose)
                {
                    Console.Error.Write(eData.PropertyData);
                }
            }
            else if (ViewModelService.MainViewModel.TallyIsRunning == false)
            {
                if (e != null && e.PropertyName == nameof(ViewModelService.MainViewModel.Output))
                {
                    Console.WriteLine(ViewModelService.MainViewModel.Output);

                    if (verbose)
                        Console.Error.WriteLine("Tally completed!");
                }
            }
        }
        #endregion
    }
}