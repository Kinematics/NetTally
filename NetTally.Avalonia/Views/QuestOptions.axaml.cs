using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DynamicData;
using Microsoft.Extensions.Logging;
using NetTally.Collections;
using NetTally.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NetTally.Avalonia.Views
{
    public class QuestOptions : Window
    {
        #region Private Properties
        private ILogger<QuestOptions> Logger { get; }
        private IQuest Quest { get; }
        private IEnumerable<IQuest> QuestList { get; }
        #endregion        

        public QuestOptions(IQuest quest, ILogger<QuestOptions> logger, QuestCollection questList)
        {
            this.Quest = quest;
            this.Logger = logger;

            // filter out the current quest.
            this.QuestList = questList.Where(q => q != quest);

            DataContext = this.Quest;

            AvaloniaXamlLoader.Load(this);

            this.FindControl<TextBox>("ThreadUrl").Text = this.Quest.ThreadName;

            this.FindControl<ComboBox>("AvailableQuests").Items = this.QuestList;
            this.FindControl<ComboBox>("AvailableQuests").SelectedItem = this.QuestList.FirstOrDefault();

#if DEBUG
            this.AttachDevTools();
#endif
        }

        #region Window element event handlers

        /// <summary>
        /// Checks for changes to the <see cref="TextBox"/>.Text property. If the text has changed
        /// to a well formed URL, then we set the thread to the URL, and remove the error class
        /// otherwise we do nothing, and set an error class.
        /// </summary>
        /// <param name="sender">The object that sent this method. Should always be <see cref="TextBox"/>.</param>
        /// <param name="e">Arguments for this event.</param>
        public void ThreadUrl_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.TextProperty 
                && e.NewValue is string newUrl
                && sender is TextBox textBox
                && textBox.Name == "ThreadUrl")
            {
                if (Uri.IsWellFormedUriString(newUrl, UriKind.Absolute)
                    && !string.IsNullOrWhiteSpace(newUrl))
                {
                    this.Quest.ThreadName = newUrl;
                    ((TextBox)sender).Classes.Remove("error");
                } else
                {
                    ((TextBox)sender).Classes.Add("error");
                }
            }
        }

        public void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            this.FindControl<TextBox>("customPostFilters").Text = "";
            this.FindControl<TextBox>("customTaskFilters").Text = "";
            this.FindControl<TextBox>("customThreadmarkFilters").Text = "";
            this.FindControl<TextBox>("customUsernameFilters").Text = "";

            this.FindControl<CheckBox>("useCustomPostFilters").IsChecked = false;
            this.FindControl<CheckBox>("useCustomTaskFilters").IsChecked = false;
            this.FindControl<CheckBox>("useCustomThreadmarkFilters").IsChecked = false;
            this.FindControl<CheckBox>("useCustomUsernameFilters").IsChecked = false;

            this.Logger.LogDebug("Quest filters have been reset.");

            this.FindControl<CheckBox>("forbidVoteLabelPlanNames").IsChecked = false;
            this.FindControl<CheckBox>("whitespaceAndPunctuationIsSignificant").IsChecked = false;
            this.FindControl<CheckBox>("caseIsSignificant").IsChecked = false;
            this.FindControl<CheckBox>("disableProxyVotes").IsChecked = false;
            this.FindControl<CheckBox>("forcePinnedProxyVotes").IsChecked = false;
            this.FindControl<CheckBox>("ignoreSpoilers").IsChecked = false;
            this.FindControl<CheckBox>("trimExtendedText").IsChecked = false;            

            this.Logger.LogDebug("Quest options have been reset.");
        }

        public void AddLinkedQuest_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindControl<ComboBox>("AvailableQuests").SelectedItem is IQuest selectedQuest)
            {
                this.Quest.AddLinkedQuest(selectedQuest);
            }
        }

        public void RemoveLinkedQuest_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindControl<ListBox>("LinkedQuests").SelectedItem is IQuest selectedQuest)
            {
                this.Quest.RemoveLinkedQuest(selectedQuest);
            }
        }

        public void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(false);
        }

        public void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(true);
        }
        #endregion

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        /// <summary>
        /// A blank constructor is needed for Avalonia Windows. It should never be called.
        /// </summary>
        public QuestOptions() { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
