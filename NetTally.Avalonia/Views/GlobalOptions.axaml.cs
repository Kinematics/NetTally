using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging;
using System;

namespace NetTally.Avalonia.Views
{

    /// <summary>
    /// Window that handles modifying global options.
    /// </summary>
    public class GlobalOptions : Window
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalOptions"/> class.
        /// </summary>
        /// <param name="options">The global options being modified. Acts as the datacontext for now.</param>
        /// <param name="logger">An appropriate ILogger for this window.</param>
        public GlobalOptions(Options.IGlobalOptions options, ILogger<GlobalOptions> logger)
        {
            this.Logger = logger;

            AvaloniaXamlLoader.Load(this);

#if DEBUG
            this.AttachDevTools();
#endif

            DataContext = options;

            // not sure why the setting in XAML isn't being respected, but resetting this here appears to work.
            this.SizeToContent = SizeToContent.WidthAndHeight;
        }
        #endregion

        #region Window element event handlers

        /// <summary>
        /// Closes the Window
        /// </summary>
        /// <param name="sender">Control sending this command</param>
        /// <param name="e">Event arguments</param>
        public void Close_Click(object sender, RoutedEventArgs e) => Close();

        /// <summary>
        /// Resets all parameters to their default values.
        /// </summary>
        /// <remarks>
        /// This logic should probably move to the model.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ResetAll_Click(object sender, RoutedEventArgs e)
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

        #region Private Properties

        /// <summary>
        /// Gets the logger.
        /// </summary>
        private ILogger<GlobalOptions> Logger { get; }

        #endregion

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        /// <summary>
        /// A blank constructor is needed for Avalonia Windows. It should never be called.
        /// </summary>
        public GlobalOptions() { throw new InvalidOperationException("The default constructor should not be called"); }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
