using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally
{
    public class CheckForNewRelease : INotifyPropertyChanged
    {
        public bool newRelease = false;

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
        /// </summary>
        private async Task DoVersionCheck()
        {
            Task<string> latestVersionTask = GetLatestVersion();
            string currentVersion = GetCurrentVersion();

            string latestVersion = await latestVersionTask;

            if (latestVersion == null)
                return;

            if (latestVersion.CompareTo(currentVersion) > 0)
                NewRelease = true;
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

            string releaseText = h1ReleaseTitle.InnerText.Trim();

            if (releaseText.StartsWith("v"))
                releaseText = releaseText.Substring(1);

            int spaceIndex = releaseText.IndexOf(" ");

            if (spaceIndex > 0)
            {
                releaseText = releaseText.Substring(0, spaceIndex);
            }

            return releaseText;
        }

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
