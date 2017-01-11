using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Extensions;
using NetTally.ViewModels;
using NetTally.Web;

namespace NetTally.Utility
{
    public class CheckForNewRelease : INotifyPropertyChanged
    {
        bool newRelease = false;
        static readonly Regex potentialVersionRegex = new Regex(@"[^.](?<version>\d+(\.\d+){0,3})");

        #region Property event handling.  Notify the main window when this value changes.
        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Bool indicating whether there is a newer release available.
        /// </summary>
        public bool NewRelease
        {
            get
            {
                return newRelease;
            }
            set
            {
                newRelease = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Do the version check update.  Rerun the check every two days, if the program is kept open that long.
        /// </summary>
        public async Task Update()
        {
            while (true)
            {
                try
                {
                    await DoVersionCheckAsync().ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromDays(2)).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    ErrorLog.Log(e);
                }
            }
        }
        #endregion

        #region Private version checking methods
        /// <summary>
        /// Check to see if there's a newer release than the currently running version.
        /// If a newer version is found, the NewRelease property is set to true.
        /// If no newer version is found, it sets up a request to re-run this function in 2 days time.
        /// </summary>
        /// <returns>Returns nothing.  Just runs async.</returns>
        private async Task DoVersionCheckAsync()
        {
            Version currentVersion = ProductInfo.FileVersion;
            Version latestVersion = await GetLatestVersionAsync().ConfigureAwait(false);

            if (currentVersion == null || latestVersion == null)
                return;

            if (latestVersion.CompareTo(currentVersion) > 0)
            {
                NewRelease = true;
            }
        }

        /// <summary>
        /// Get the current program version information, to compare with the latest version info.
        /// </summary>
        /// <returns>Returns the current version string.</returns>
        private async Task<Version> GetLatestVersionAsync()
        {
            Version latestVersion = null;

            string latestVersionString = await GetLatestVersionStringAsync().ConfigureAwait(false);

            if (!string.IsNullOrEmpty(latestVersionString))
                latestVersion = new Version(latestVersionString);

            return latestVersion;
        }

        /// <summary>
        /// Examine the releases web page for the latest release, to get 
        /// the version number of the latest release.
        /// </summary>
        /// <returns>Returns the latest version string.</returns>
        private async Task<string> GetLatestVersionStringAsync()
        {
            HtmlDocument htmldoc = await GetLatestReleasePageAsync().ConfigureAwait(false);

            if (htmldoc != null)
            {
                var latest = htmldoc.DocumentNode.GetDescendantWithClass("div", "label-latest");

                if (latest != null)
                {
                    var h1ReleaseTitle = htmldoc.DocumentNode.GetDescendantWithClass("h1", "release-title");

                    if (h1ReleaseTitle != null)
                    {
                        return GetVersionString(h1ReleaseTitle.InnerText);
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Get the Github page that contains the latest release.
        /// </summary>
        /// <returns>Returns the HTML document for the requested page,
        /// or null if it fails to load.</returns>
        private async Task<HtmlDocument> GetLatestReleasePageAsync()
        {
            HtmlDocument doc = null;
            string url = "https://github.com/Kinematics/NetTally/releases/latest";

            doc = await ViewModelService.MainViewModel.PageProvider.GetPage(url, "Github Releases", CachingMode.BypassCache, ShouldCache.Yes,
                SuppressNotifications.Yes, CancellationToken.None).ConfigureAwait(false);

            return doc;
        }

        /// <summary>
        /// Extract a version string from the provided string (text from web page).
        /// </summary>
        /// <param name="potentialVersion">Web page text that's expected to hold a version number.</param>
        /// <returns>Returns the version as a string, if available.</returns>
        private static string GetVersionString(string potentialVersion)
        {
            Match m = potentialVersionRegex.Match(potentialVersion);
            if (m.Success)
            {
                return m.Groups["version"].Value;
            }

            return "";
        }
        #endregion
    }
}
