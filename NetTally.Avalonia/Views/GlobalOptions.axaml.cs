using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging;
using NetTally.Options;
using NetTally.ViewModels;

namespace NetTally.Avalonia.Views
{
    public class GlobalOptions : Window
    {
        #region Setup and construction
        private ILogger<GlobalOptions> Logger { get; }

        public GlobalOptions() { }

        public GlobalOptions(IGlobalOptions options, ILogger<GlobalOptions> logger)
        {
            this.Logger = logger;

#if DEBUG
            this.AttachDevTools();
#endif

            AvaloniaXamlLoader.Load(this);

            DataContext = options;
        }
        #endregion

        #region Window element event handlers
        public void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void ResetAllButton_Click(object sender, RoutedEventArgs e)
        {
            this.FindControl<ComboBox>("RankedVoteAlgorithm").SelectedIndex = 0;
            this.FindControl<CheckBox>("AllowUsersToUpdatePlans").IsChecked = null;
            this.FindControl<CheckBox>("TrackPostAuthorsUniquely").IsChecked = false;
            this.FindControl<CheckBox>("GlobalSpoilers").IsChecked = false;
            this.FindControl<CheckBox>("DisplayPlansWithNoVotes").IsChecked = false;
            this.FindControl<CheckBox>("DebugMode").IsChecked = false;
            this.FindControl<CheckBox>("DisableWebProxy").IsChecked = false;

            Logger.LogDebug("Global options have been reset.");
        }
        #endregion
    }
}
