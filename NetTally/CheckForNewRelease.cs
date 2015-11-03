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
        public bool newRelease = false;
        static readonly Regex potentialVersionRegex = new Regex(@"[^.](?<version>\d+(\.\d+){0,3})");
        static readonly Regex numbers = new Regex(@"\d+");


        /// <summary>
        /// Constructor
        /// </summary>
        public CheckForNewRelease()
        {
            // Disable warning about needing to use 'await' on an async method.
#pragma warning disable 4014
            DoVersionCheck();
#pragma warning restore 4014
        }

        /// <summary>
        /// Run a version check when constructing this class.
        /// Compare the current program version with the latest available version on Github.
        /// If the latest version is newer than the current version, update the NewRelease property.
        /// Repeat the check once per hour, until a connection is made to the Github page,
        /// and then once every two days until a new version is found.
        /// </summary>
        private async Task DoVersionCheck()
        {
            string currentVersion = GetCurrentVersion();

            while (NewRelease == false)
            {
                string latestVersion = await GetLatestVersion().ConfigureAwait(false);

                // If the attempt to determine the latest version fails, try again after an hour.
                if (latestVersion == null || latestVersion == string.Empty)
                {
                    await Task.Delay(TimeSpan.FromHours(1)).ConfigureAwait(false);
                    continue;
                }

                NewRelease = IsLatestVersionNewer(currentVersion, latestVersion);

                if (NewRelease == false)
                {
                    // If no new release was found, try again in a couple days.
                    await Task.Delay(TimeSpan.FromDays(2)).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Compare two version strings to see which one is 'higher', given that
        /// each dotted number increments individually, and may go from, say, 
        /// 9 to 10 (eg: 1.1.9 to 1.1.10).
        /// </summary>
        /// <param name="currentVersion">The string for the current program version.</param>
        /// <param name="latestVersion">The string for the latest program version.</param>
        /// <returns>Returns true if the latestVersion value is 'higher' than the currentVersion.</returns>
        private bool IsLatestVersionNewer(string currentVersion, string latestVersion)
        {
            if (currentVersion == null || latestVersion == null)
                return false;

            int currentVersionNumber;
            int latestVersionNumber;

            Match mCurrent = numbers.Match(currentVersion);
            Match mLatest = numbers.Match(latestVersion);

            while (mCurrent.Success && mLatest.Success)
            {
                if (int.TryParse(mCurrent.Value, out currentVersionNumber) &&
                    int.TryParse(mLatest.Value, out latestVersionNumber))
                {
                    if (latestVersionNumber > currentVersionNumber)
                        return true;
                    if (currentVersionNumber > latestVersionNumber)
                        return false;
                }

                mCurrent = mCurrent.NextMatch();
                mLatest = mLatest.NextMatch();
            }

            // If the above loop ended, but the latest version's match still has more
            // valid number entries, that means it's 'higher' than the current version.
            // EG: 1.1.9 vs 1.1.9.1
            return mLatest.Success;
        }

        /// <summary>
        /// Get the current program version information, to compare with the latest version info.
        /// </summary>
        /// <returns>Returns the current version string.</returns>
        private string GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var product = (AssemblyProductAttribute)assembly.GetCustomAttribute(typeof(AssemblyProductAttribute));
            var version = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));

            return version.InformationalVersion.Trim();
        }

        /// <summary>
        /// Examine the web page for the latest release, to get the version number of the latest release.
        /// </summary>
        /// <returns>Returns the latest version string.</returns>
        private async Task<string> GetLatestVersion()
        {
            HtmlDocument htmldoc = await GetLatestReleasePage();

            if (htmldoc == null)
                return string.Empty;

            var h1ReleaseTitle = htmldoc.DocumentNode.Descendants("h1").FirstOrDefault(n => n.GetAttributeValue("class", "").Contains("release-title"));

            if (h1ReleaseTitle == null)
                return string.Empty;

            return GetVersionString(h1ReleaseTitle.InnerText);
        }

        /// <summary>
        /// Get the Github page that contains the latest release.
        /// </summary>
        /// <returns>Returns the HTML document for the requested page,
        /// or null if it fails to load.</returns>
        private async Task<HtmlDocument> GetLatestReleasePage()
        {
            IPageProvider webPageProvider = new WebPageProvider();

            string url = "https://github.com/Kinematics/NetTally/releases/latest";

            try
            {
                return await webPageProvider.GetPage(url, "", Caching.BypassCache, CancellationToken.None);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extract a version string from the provided string (text from web page).
        /// </summary>
        /// <param name="potentialVersion">Web page text that's expected to hold a version number.</param>
        /// <returns>Returns the version as a string, if available.</returns>
        public string GetVersionString(string potentialVersion)
        {
            Match m = potentialVersionRegex.Match(potentialVersion);
            if (m.Success)
            {
                return m.Groups["version"].Value;
            }

            return "";
        }


        #region Property event handling
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
            get { return newRelease; }
            set
            {
                newRelease = value;
                OnPropertyChanged();
            }
        }
        #endregion
    }
}
