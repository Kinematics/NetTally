using System;
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

        #region Constructor

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

            this.globalOptionsViewModel.SaveCompleted += GlobalOptionsViewModel_SaveCompleted;

            //AvaloniaXamlLoader.Load(this);
            InitializeComponent();

            DataContext = this.globalOptionsViewModel;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            globalOptionsViewModel.SaveCompleted -= GlobalOptionsViewModel_SaveCompleted;
            base.OnClosing(e);
        }

        private void GlobalOptionsViewModel_SaveCompleted()
        {
            logger.LogDebug("Global options were saved.");
            Close();
        }

        #endregion

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        /// <summary>
        /// A blank constructor is needed for Avalonia Windows. It should never be called.
        /// </summary>
        public GlobalOptions() { throw new InvalidOperationException("The default constructor should not be called"); }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
