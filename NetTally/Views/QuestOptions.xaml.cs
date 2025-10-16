using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using NetTally.Configure;
using NetTally.ViewModels;

namespace NetTally.Views
{
    /// <summary>
    /// Interaction logic for QuestOptions2.xaml
    /// </summary>
    public partial class QuestOptions : Window
    {
        private readonly QuestOptionsViewModel questOptionsViewModel;
        private readonly ILogger<QuestOptions> logger;
        private readonly string clipboardUrl;

        public QuestOptions(
            QuestOptionsViewModel questOptionsViewModel,
            ILogger<QuestOptions> logger,
            string url = "")
        {
            this.questOptionsViewModel = questOptionsViewModel;
            this.logger = logger;
            this.clipboardUrl = url;

            this.questOptionsViewModel.PropertyChanged += QuestOptionsViewModel_PropertyChanged;
            questOptionsViewModel.SetQuestThreadFromClipboard(clipboardUrl);

            InitializeComponent();
            DataContext = this.questOptionsViewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (questOptionsViewModel.ThreadName == Strings.NewThreadEntry)
            {
                QuestUrlBox.Focus();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            questOptionsViewModel.PropertyChanged -= QuestOptionsViewModel_PropertyChanged;
            base.OnClosed(e);
        }

        private void TextEntry_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }

        private void TextEntry_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (!tb.IsKeyboardFocusWithin)
                {
                    tb.Focus();
                    e.Handled = true;
                }
            }
        }

        private void QuestOptionsViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(questOptionsViewModel.SaveCommand))
            {
                logger.LogDebug("Quest options were saved.");
                DialogResult = true;
                Close();
            }
            else if (e.PropertyName == nameof(questOptionsViewModel.ResetCommand))
            {
                questOptionsViewModel.SetQuestThreadFromClipboard(clipboardUrl);
            }
            else if (e.PropertyName == nameof(questOptionsViewModel.CancelCommand))
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
