﻿using System.Windows;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for GlobalOptionsWindow.xaml
    /// </summary>
    public partial class GlobalOptionsWindow : Window
    {
        public GlobalOptionsWindow()
        {
            InitializeComponent();
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void resetAllButton_Click(object sender, RoutedEventArgs e)
        {
            allowRankedVotes.IsChecked = true;
            rankedVoteAlgorithm.SelectedIndex = 0;

            globalSpoilers.IsChecked = false;

            debugMode.IsChecked = false;
        }
    }
}
