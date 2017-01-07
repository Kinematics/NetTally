﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using NetTally.ViewModels;
using CommandLine;

namespace NetTally.CLI
{
    class Program
    {
        #region Main entry point
        static void Main(string[] args)
        {
            ViewModelService.Instance.Build();

            var results = Parser.Default.ParseArguments<Options>(args);

            results.WithParsed(o => RunWithOptions(o))
                .WithNotParsed(e => DealWithErrors(e));
        }
        #endregion

        #region Variables
        static ManualResetEventSlim waiting = new ManualResetEventSlim(false);
        static bool verbose = false;
        #endregion

        #region General running code
        /// <summary>
        /// Function that initiates the tally using the provided options.
        /// </summary>
        /// <param name="options"></param>
        private static void RunWithOptions(Options options)
        {
            verbose = options.Verbose;

            SetGlobalOptions(options);

            Quest quest = GetQuestWithOptions(options);

            RunTally(quest);

            // Wait for a PropertyChanged update signal indicating the tally was completed, or a timeout.
            waiting.Wait(TimeSpan.FromSeconds(60));

            if (!waiting.IsSet && !verbose)
            {
                Console.Write("Tally attempt failed.");
            }
        }

        /// <summary>
        /// Handle output if commandline argument parsing failed.
        /// </summary>
        /// <param name="e">The list of errors generated.</param>
        private static void DealWithErrors(IEnumerable<Error> e)
        {
            //Console.WriteLine(Options.GetUsage());
        }

        /// <summary>
        /// Run the tally for the provided quest.
        /// </summary>
        /// <param name="quest">The quest to run the tally on.</param>
        private static void RunTally(Quest quest)
        {
            if (ViewModelService.MainViewModel.AddQuestCommand.CanExecute(null))
            {
                ViewModelService.MainViewModel.AddQuestCommand.Execute(quest);
                ViewModelService.MainViewModel.SelectedQuest = quest;

                ViewModelService.MainViewModel.PropertyChanged += MainViewModel_PropertyChanged;

                if (ViewModelService.MainViewModel.RunTallyCommand.CanExecute(null))
                {
                    ViewModelService.MainViewModel.RunTallyCommand.Execute(null);
                }
            }
        }

        /// <summary>
        /// Gets a quest instance, set according to the provided commandline options.
        /// </summary>
        /// <param name="options">Custom options to set on the quest.</param>
        /// <returns>Returns a completed Quest instance.</returns>
        private static Quest GetQuestWithOptions(Options options)
        {
            Quest quest = new Quest()
            {
                StartPost = options.StartPost,
                EndPost = options.EndPost,
                ThreadName = options.Thread,
                CheckForLastThreadmark = options.Threadmark,
                CustomThreadmarkFilters = options.ThreadmarkFilters,
                CustomTaskFilters = options.TaskFilters,
                PartitionMode = options.PartitionMode
            };

            if (!string.IsNullOrEmpty(options.ThreadmarkFilters))
                quest.UseCustomThreadmarkFilters = true;

            if (!string.IsNullOrEmpty(options.TaskFilters))
                quest.UseCustomTaskFilters = true;

            return quest;
        }

        /// <summary>
        /// Sets the global service options on the main view model.
        /// </summary>
        /// <param name="options">The commandline options set when run.</param>
        private static void SetGlobalOptions(Options options)
        {
            ViewModelService.MainViewModel.Options.AllowRankedVotes = !options.NoRanks;

            ViewModelService.MainViewModel.Options.DisplayMode = options.DisplayMode;
            ViewModelService.MainViewModel.Options.DisableProxyVotes = options.NoProxy;
            ViewModelService.MainViewModel.Options.ForbidVoteLabelPlanNames = options.ForbidPlanLabels;
            ViewModelService.MainViewModel.Options.ForcePinnedProxyVotes = options.PinProxy;
            ViewModelService.MainViewModel.Options.IgnoreSpoilers = options.NoSpoilers;
            ViewModelService.MainViewModel.Options.WhitespaceAndPunctuationIsSignificant = options.Whitespace;
            ViewModelService.MainViewModel.Options.TrimExtendedText = options.Trim;

            ViewModelService.MainViewModel.Options.DebugMode = options.Debug;
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
            if (ViewModelService.MainViewModel.TallyIsRunning &&
                e.PropertyName == nameof(ViewModelService.MainViewModel.OutputChanging) && 
                verbose)
            {
                Console.Write(ViewModelService.MainViewModel.OutputChanging);
            }
            else if (ViewModelService.MainViewModel.TallyIsRunning == false && 
                     e.PropertyName == nameof(ViewModelService.MainViewModel.Output))
            {
                if (verbose)
                    Console.WriteLine();

                Console.WriteLine(ViewModelService.MainViewModel.Output);

                waiting.Set();
            }
        }
        #endregion
    }
}