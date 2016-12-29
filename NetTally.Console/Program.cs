using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using NetTally.ViewModels;
using NetTally.Platform;
using NetTally.Utility;
using CommandLine;

namespace NetTally.CLI
{
    class Program
    {
        static ManualResetEventSlim waiting = new ManualResetEventSlim(false);
        static bool verbose = false;

        static void Main(string[] args)
        {
            Agnostic.InitStringComparers(UnicodeHashFunction.HashFunction);

            if (args.Any())
            {
                var results = Parser.Default.ParseArguments<Options>(args);

                results.WithParsed(o => RunWithOptions(o))
                    .WithNotParsed(e => DealWithErrors(e));
            }
            else
            {
                DealWithErrors(null);
            }

            waiting.Wait(TimeSpan.FromSeconds(60));
        }

        private static void DealWithErrors(IEnumerable<Error> e)
        {
            Console.WriteLine(Options.GetUsage());
        }

        private static void RunWithOptions(Options options)
        {
            ViewModelService.Instance.Build();

            Quest quest = new Quest() {
                StartPost = options.StartPost,
                EndPost = options.EndPost,
                ThreadName = options.Thread,
                CheckForLastThreadmark = options.Threadmark };

            ViewModelService.MainViewModel.Options.AllowRankedVotes = true;
            ViewModelService.MainViewModel.Options.DisplayMode = Output.DisplayMode.SpoilerVoters;

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
        }

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
    }
}