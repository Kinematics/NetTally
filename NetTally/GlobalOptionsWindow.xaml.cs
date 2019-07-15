﻿using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using NetTally.Navigation;
using NetTally.ViewModels;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for the global options window.
    /// </summary>
    public partial class GlobalOptionsWindow : Window, IActivable
    {
        readonly ILogger<GlobalOptionsWindow> logger;

        public GlobalOptionsWindow(MainViewModel model, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<GlobalOptionsWindow>();

            InitializeComponent();

            DataContext = model;
        }

        public Task ActivateAsync(object? parameter)
        {
            if (parameter is Window owner)
            {
                this.Owner = owner;
            }

            return Task.CompletedTask;
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void resetAllButton_Click(object sender, RoutedEventArgs e)
        {
            rankedVoteAlgorithm.SelectedIndex = 0;
            allowRankedVotes.IsChecked = true;
            globalSpoilers.IsChecked = false;
            debugMode.IsChecked = false;

            logger.LogDebug("Global options have been reset.");
        }
    }
}
