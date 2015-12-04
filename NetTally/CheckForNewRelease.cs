using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally
{
    public class CheckForNewRelease : INotifyPropertyChanged
    {
        bool newRelease = false;
        static readonly Regex potentialVersionRegex = new Regex(@"[^.](?<version>\d+(\.\d+){0,3})");
        static readonly Regex numbers = new Regex(@"\d+");

        /// <summary>
        /// Constructor
        /// </summary>
        public CheckForNewRelease()
        {
        }

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
        /// Do the version check update.
        /// </summary>
        public void Update()
        {
            // Store result in a task, but don't await it.
            var result = DoVersionCheck();
        }
        #endregion

        #region Private version checking methods
        /// <summary>
        /// Check to see if there's a newer release than the currently running version.
        /// If a newer version is found, the NewRelease property is set to true.
        /// If no newer version is found, it sets up a request to re-run this function in 2 days time.
        /// </summary>
        /// <returns>Returns nothing.  Just runs async.</returns>
        private async Task DoVersionCheck()
        {
            try
            {
                Version currentVersion = GetCurrentVersion();
                Version latestVersion = await GetLatestVersion().ConfigureAwait(false);

                if (currentVersion == null || latestVersion == null)
                    return;

                bool newer = latestVersion.CompareTo(currentVersion) > 0;

                if (newer)
                {
                    NewRelease = newer;
                }
                else
                {
                    var delay = DelayedAction(DoVersionCheck);
                }
            }
            catch (Exception e)
            {
                ErrorLog.Log(e);
            }
        }

        /// <summary>
        /// Utility function to run an async function after a delay.
        /// Delay is set to 2 days.
        /// </summary>
        /// <param name="action">The function to run.</param>
        /// <returns>Returns nothing.  Just runs async.</returns>
        private async Task DelayedAction(Func<Task> action)
        {
            await Task.Delay(TimeSpan.FromDays(2)).ConfigureAwait(false);
            var result = action();
        }

        /// <summary>
        /// Get the current program version information, to compare with the latest version info.
        /// </summary>
        /// <returns>Returns the current version.</returns>
        private Version GetCurrentVersion()
        {
            Version currentVersion = null;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fileVersion = (AssemblyFileVersionAttribute)assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute));

                currentVersion = new Version(fileVersion.Version);
            }
            catch (Exception e)
            {
                ErrorLog.Log(e);
            }

            return currentVersion;
        }

        /// <summary>
        /// Get the current program version information, to compare with the latest version info.
        /// </summary>
        /// <returns>Returns the current version string.</returns>
        private async Task<Version> GetLatestVersion()
        {
            Version latestVersion = null;

            try
            {
                string latestVersionString = await GetLatestVersionString().ConfigureAwait(false);

                if (!string.IsNullOrEmpty(latestVersionString))
                    latestVersion = new Version(latestVersionString);
            }
            catch (Exception e)
            {
                ErrorLog.Log(e);
            }

            return latestVersion;
        }

        /// <summary>
        /// Examine the releases web page for the latest release, to get 
        /// the version number of the latest release.
        /// </summary>
        /// <returns>Returns the latest version string.</returns>
        private async Task<string> GetLatestVersionString()
        {
            try
            {
                HtmlDocument htmldoc = await GetLatestReleasePage().ConfigureAwait(false);

                if (htmldoc != null)
                {
                    var h1ReleaseTitle = htmldoc.DocumentNode.Descendants("h1")?.FirstOrDefault(n => n.GetAttributeValue("class", "").Contains("release-title"));

                    if (h1ReleaseTitle != null)
                    {
                        return GetVersionString(h1ReleaseTitle.InnerText);
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLog.Log(e);
            }

            return string.Empty;
        }

        /// <summary>
        /// Get the Github page that contains the latest release.
        /// </summary>
        /// <returns>Returns the HTML document for the requested page,
        /// or null if it fails to load.</returns>
        private async Task<HtmlDocument> GetLatestReleasePage()
        {
            HtmlDocument doc = null;
            string url = "https://github.com/Kinematics/NetTally/releases/latest";

            try
            {
                IPageProvider webPageProvider = new WebPageProvider();

                doc = await webPageProvider.GetPage(url, "", Caching.BypassCache, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ErrorLog.Log(e);
            }

            return doc;
        }

        /// <summary>
        /// Extract a version string from the provided string (text from web page).
        /// </summary>
        /// <param name="potentialVersion">Web page text that's expected to hold a version number.</param>
        /// <returns>Returns the version as a string, if available.</returns>
        private string GetVersionString(string potentialVersion)
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
