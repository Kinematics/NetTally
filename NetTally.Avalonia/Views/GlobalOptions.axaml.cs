using System;
using System.ComponentModel;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
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
        }

        protected override void OnClosed(EventArgs e)
        {
            this.globalOptionsViewModel.PropertyChanged -= GlobalOptionsViewModel_PropertyChanged;
            base.OnClosed(e);
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
