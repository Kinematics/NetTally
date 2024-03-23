using System;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using NetTally.ViewModels;

namespace NetTally.Avalonia.Views
{
    public partial class QuestOptions : Window
    {
        #region Private Properties
        private readonly ILogger<QuestOptions> logger;
        private readonly QuestOptionsViewModel questOptionsViewModel;
        #endregion        

        public QuestOptions(
            QuestOptionsViewModel viewModel,
            ILogger<QuestOptions> logger,
            string? label = "")
        {
            questOptionsViewModel = viewModel;
            this.logger = logger;

            questOptionsViewModel.SaveCompleted += QuestOptionsViewModel_SaveCompleted;
            DataContext = questOptionsViewModel;

            //AvaloniaXamlLoader.Load(this);
            InitializeComponent();

#if DEBUG
            this.AttachDevTools();
#endif
        }

        #region Window element event handlers

        // Idealy I would like to change all of these into standard functions or commands for better
        // type safety.

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            questOptionsViewModel.SaveCompleted -= QuestOptionsViewModel_SaveCompleted;
            base.OnClosing(e);
        }

        private void QuestOptionsViewModel_SaveCompleted()
        {
            logger.LogDebug("Global options were saved.");
            Close();
        }

        #endregion

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        /// <summary>
        /// A blank constructor is needed for Avalonia Windows. It should never be called.
        /// </summary>
        public QuestOptions() { throw new InvalidOperationException("The default constructor should not be called"); }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
