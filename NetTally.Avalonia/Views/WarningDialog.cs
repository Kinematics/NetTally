using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using System.Threading.Tasks;

namespace NetTally.Avalonia.Views
{

    /// <summary>
    /// Simple class for handling Warning Dialogs.
    /// </summary>
    public static class WarningDialog
    {
        /// <summary>
        /// Simplified handling of showing a warning for errors that have been logged.
        /// </summary>
        /// <param name="primaryMessage">The main text to show before showing where the logs have been saved.</param>
        /// <param name="title">The text to use as the title of the message box.</param>
        public static Task<ButtonResult> Show(string primaryMessage, string title, bool logsSaved = true)
        {
            primaryMessage += (logsSaved) ? $"\nLogs have been saved." : "";

            return MessageBoxManager.GetMessageBoxStandardWindow(StandardParamGenerator(title, primaryMessage)).Show();
        }

        private static MessageBoxStandardParams StandardParamGenerator(string title, string message) =>
            new MessageBoxStandardParams { ContentTitle = title, ContentMessage = message };
    }
}
