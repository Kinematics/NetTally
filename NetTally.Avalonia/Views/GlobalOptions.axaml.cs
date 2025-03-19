using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Styling;
using Microsoft.Extensions.Logging;
using NetTally.Enums;
using NetTally.ViewModels;

namespace NetTally.Avalonia.Views
{
    /// <summary>
    /// Window that handles modifying global options.
    /// </summary>
    public partial class GlobalOptions : Window
    {
        private readonly GlobalOptionsViewModel globalOptionsViewModel;
        private readonly ILogger<GlobalOptions> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalOptions"/> class.
        /// </summary>
        /// <param name="options">The global options being modified. Acts as the datacontext for now.</param>
        /// <param name="logger">An appropriate ILogger for this window.</param>
        public GlobalOptions(
            GlobalOptionsViewModel globalOptionsViewModel,
            ILogger<GlobalOptions> logger)
        {
            this.globalOptionsViewModel = globalOptionsViewModel;
            this.logger = logger;

            this.globalOptionsViewModel.PropertyChanged += GlobalOptionsViewModel_PropertyChanged;

            InitializeComponent();

            DataContext = this.globalOptionsViewModel;
        }

        private void GlobalOptionsViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(globalOptionsViewModel.SaveCommand))
            {
                logger.LogDebug("Global options were saved.");
                Close();
            }
            else if (e.PropertyName == nameof(globalOptionsViewModel.ThemeVariant))
            {
                ApplyRequestedTheme();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            this.globalOptionsViewModel.PropertyChanged -= GlobalOptionsViewModel_PropertyChanged;
            base.OnClosed(e);
        }


        private AvaloniaTheme GetCurrentTheme()
        {
            if (App.Current!.ActualThemeVariant == ThemeVariant.Light)
                return AvaloniaTheme.Light;
            if (App.Current!.ActualThemeVariant == ThemeVariant.Dark)
                return AvaloniaTheme.Dark;
            return AvaloniaTheme.Default;
        }

        private void ApplyRequestedTheme()
        {
            var current = GetCurrentTheme();
            if (current == globalOptionsViewModel.ThemeVariant)
                return;

            logger.LogDebug("Requested a change in theme from {before} to {after}",
                current, globalOptionsViewModel.ThemeVariant);

            App.Current!.RequestedThemeVariant = globalOptionsViewModel.ThemeVariant switch
            {
                AvaloniaTheme.Light => ThemeVariant.Light,
                AvaloniaTheme.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }


#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
#if DEBUG
        /// <summary>
        /// A blank constructor is needed for Avalonia Windows. It should never be called.
        /// </summary>
        public GlobalOptions()
        {
            InitializeComponent();
        }
#endif
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
