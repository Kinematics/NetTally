using System;
using System.ComponentModel;
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
        private readonly string clipboardUrl;
        #endregion        

        public QuestOptions(
            QuestOptionsViewModel viewModel,
            ILogger<QuestOptions> logger,
            string url = "")
        {
            questOptionsViewModel = viewModel;
            this.logger = logger;
            clipboardUrl = url;

            questOptionsViewModel.PropertyChanged += QuestOptionsViewModel_PropertyChanged;
            questOptionsViewModel.SetQuestThreadFromClipboard(clipboardUrl);

            InitializeComponent();
            DataContext = questOptionsViewModel;

#if DEBUG
            this.AttachDevTools();
#endif
        }

        #region View Model event handlers
        private void QuestOptionsViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(questOptionsViewModel.SaveCommand))
            {
                logger.LogDebug("Quest options were saved.");
                Close();
            }
            else if (e.PropertyName == nameof(questOptionsViewModel.ResetCommand))
            {
                questOptionsViewModel.SetQuestThreadFromClipboard(clipboardUrl);
            }
        }
        #endregion View Model event handlers

        #region Window element event handlers
        protected override void OnClosing(WindowClosingEventArgs e)
        {
            questOptionsViewModel.PropertyChanged -= QuestOptionsViewModel_PropertyChanged;
            base.OnClosing(e);
        }

        private void ThreadUrl_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        /// <summary>
        /// A blank constructor is needed for Avalonia Windows. It should never be called.
        /// </summary>
        public QuestOptions() 
        {
            //throw new InvalidOperationException("The default constructor should not be called");
        }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
