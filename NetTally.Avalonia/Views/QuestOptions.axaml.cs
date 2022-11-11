using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NetTally.Avalonia.Views
{
    public class QuestOptions : Window
    {
        #region Private Properties
        private ILogger<QuestOptions> Logger { get; }
        private IQuest Quest { get; }
        private IQuest ShadowCopy { get; }
        private IEnumerable<IQuest> QuestList { get; }
        #endregion        

        public QuestOptions(IQuest quest, ILogger<QuestOptions> logger, Collections.QuestCollection questList)
        {
            this.Quest = quest;
            this.ShadowCopy = quest.GetShadowCopy();
            this.Logger = logger;

            // filter out the current quest.
            this.QuestList = questList.Where(q => q != quest);

            DataContext = this.ShadowCopy;

            AvaloniaXamlLoader.Load(this);

            this.FindControl<TextBox>("ThreadUrl").Text = this.ShadowCopy.ThreadName;

            this.FindControl<ComboBox>("AvailableQuests").Items = this.QuestList;
            this.FindControl<ComboBox>("AvailableQuests").SelectedItem = this.QuestList.FirstOrDefault();

#if DEBUG
            this.AttachDevTools();
#endif
        }

        #region Window element event handlers

        // Idealy I would like to change all of these into standard functions or commands for better
        // type safety.

        /// <summary>
        /// Checks for changes to the <see cref="TextBox"/>.Text property. If the text has changed
        /// to a well formed URL, then we set the thread to the URL, and remove the error class
        /// otherwise we do nothing, and set an error class.
        /// </summary>
        /// <remarks>
        /// This method functions like this because Quest.ThreadName will throw an exception if it gets a badly 
        /// formed URL which prevents us from binding it. Idealy Quest instead would implement some validation 
        /// logic on its property, and we could bind and reflect that, but for now we do this.
        /// </remarks>
        /// <param name="sender">The object that sent this method. Should always be <see cref="TextBox"/>.</param>
        /// <param name="e">Arguments for this event.</param>
        public void ThreadUrl_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            // Guard against events we don't want to handle, since this will trigger for all
            // PropertyChanged events on this control.
            if (e.Property != TextBox.TextProperty
                || e.NewValue is not string newUrl
                || sender is not TextBox textBox
                || textBox.Name != "ThreadUrl")
            {
                return;
            }

            // check if the string would be accepted by Quest, if so set it, and remove our error class.
            if (!string.IsNullOrWhiteSpace(newUrl) &&
                Uri.IsWellFormedUriString(newUrl, UriKind.Absolute))
            {
                this.ShadowCopy.ThreadName = newUrl;
                textBox.Classes.Remove("Error");
            } else {
                // bad string add the error class.
                textBox.Classes.Add("Error");
            }
        }

        public void Reset_Click(object sender, RoutedEventArgs e)
        {
            this.FindControl<TextBox>("CustomPostFilters").Text = "";
            this.FindControl<TextBox>("CustomTaskFilters").Text = "";
            this.FindControl<TextBox>("CustomThreadmarkFilters").Text = "";
            this.FindControl<TextBox>("CustomUsernameFilters").Text = "";

            this.FindControl<CheckBox>("UseCustomPostFilters").IsChecked = false;
            this.FindControl<CheckBox>("UseCustomTaskFilters").IsChecked = false;
            this.FindControl<CheckBox>("UseCustomThreadmarkFilters").IsChecked = false;
            this.FindControl<CheckBox>("UseCustomUsernameFilters").IsChecked = false;

            this.Logger.LogDebug("Quest filters have been reset.");

            this.FindControl<CheckBox>("WhitespaceAndPunctuationIsSignificant").IsChecked = false;
            this.FindControl<CheckBox>("CaseIsSignificant").IsChecked = false;
            this.FindControl<CheckBox>("ForcePlanReferencesToBeLabeled").IsChecked = false;
            this.FindControl<CheckBox>("ForbidVoteLabelPlanNames").IsChecked = false;
            this.FindControl<CheckBox>("AllowUsersToUpdatePlans").IsChecked = false;
            this.FindControl<CheckBox>("DisableProxyVotes").IsChecked = false;
            this.FindControl<CheckBox>("ForcePinnedProxyVotes").IsChecked = false;
            this.FindControl<CheckBox>("IgnoreSpoilers").IsChecked = false;
            this.FindControl<CheckBox>("TrimExtendedText").IsChecked = false;            

            this.Logger.LogDebug("Quest options have been reset.");
        }

        public void AddLinkedQuest_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindControl<ComboBox>("AvailableQuests").SelectedItem is IQuest selectedQuest)
            {
                this.ShadowCopy.AddLinkedQuest(selectedQuest);
            }
        }

        public void RemoveLinkedQuest_Click(object sender, RoutedEventArgs e)
        {
            if (this.FindControl<ListBox>("LinkedQuests").SelectedItem is IQuest selectedQuest)
            {
                this.ShadowCopy.RemoveLinkedQuest(selectedQuest);
            }
        }

        public void Cancel_Click(object sender, RoutedEventArgs e) => this.Close(false);

        public void Ok_Click(object sender, RoutedEventArgs e)
        {
            Quest.UpdateFromShadowCopy();
            this.Close(true);
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
