using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using NetTally.Platform;
using NetTally.Utility;
using NetTally.ViewModels;
using CommandLine;

namespace NetTally.CLI
{
    class Program
    {
        #region Startup and variables
        static ManualResetEventSlim waiting = new ManualResetEventSlim(false);
        static bool verbose = false;

        static void Main(string[] args)
        {
            Agnostic.InitStringComparers(UnicodeHashFunction.HashFunction);

            var results = Parser.Default.ParseArguments<Options>(args);

            results.WithParsed(o => RunWithOptions(o))
                .WithNotParsed(e => DealWithErrors(e));
        }

        private static void DealWithErrors(IEnumerable<Error> e)
        {
            //Console.WriteLine(Options.GetUsage());
        }
        #endregion

        #region General running code
        /// <summary>
        /// Function that initiates the tally using the provided options.
        /// </summary>
        /// <param name="options"></param>
        private static void RunWithOptions(Options options)
        {
            ViewModelService.Instance.Build();

            Quest quest = new Quest() {
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


            ViewModelService.MainViewModel.Options.AllowRankedVotes = !options.NoRanks;

            ViewModelService.MainViewModel.Options.DisplayMode = options.DisplayMode;
            ViewModelService.MainViewModel.Options.DisableProxyVotes = options.NoProxy;
            ViewModelService.MainViewModel.Options.ForbidVoteLabelPlanNames = options.ForbidPlanLabels;
            ViewModelService.MainViewModel.Options.ForcePinnedProxyVotes = options.PinProxy;
            ViewModelService.MainViewModel.Options.IgnoreSpoilers = options.NoSpoilers;
            ViewModelService.MainViewModel.Options.WhitespaceAndPunctuationIsSignificant = options.Whitespace;
            ViewModelService.MainViewModel.Options.TrimExtendedText = options.Trim;

            ViewModelService.MainViewModel.Options.DebugMode = options.Debug;

            verbose = options.Verbose;

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

            waiting.Wait(TimeSpan.FromSeconds(60));
            
            if (!waiting.IsSet && !verbose)
            {
                Console.Write("Tally attempt failed.");
            }
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
                Console.WriteLine(ViewModelService.MainViewModel.Output);

                waiting.Set();
            }
        }
        #endregion
    }
}