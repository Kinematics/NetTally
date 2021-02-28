using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using NetTally.Extensions;
using NetTally.SystemInfo;
using NetTally.Web;
using NetTally.Types.Enums;

namespace NetTally
{
    public class CheckForNewRelease : INotifyPropertyChanged
    {
        bool newRelease = false;
        readonly IPageProvider pageProvider;
        readonly ILogger<CheckForNewRelease> logger;
        static readonly string githubReleasesPage = "https://github.com/Kinematics/NetTally/releases";

        public CheckForNewRelease(IPageProvider provider, ILogger<CheckForNewRelease> logger)
        {
            pageProvider = provider;
            this.logger = logger;
        }

        #region Property event handling.  Notify the main window when this value changes.
        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
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
                    logger.LogWarning(e, "Check for new release update attempt failed.");
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
            Version latestVersion = await GetLatestVersionAsync2(currentVersion.Major).ConfigureAwait(false);

            if (currentVersion == null)
                return;

            if (latestVersion.CompareTo(currentVersion) > 0)
            {
                NewRelease = true;
            }
        }

        /// <summary>
        /// Get the latest version we can find that matches the provided
        /// current major version number.
        /// Version 2 and version 3 have different runtime requirements, so
        /// don't suggest upgrades across major versions.
        /// </summary>
        /// <param name="currentMajorVersion">The current major version number.</param>
        /// <returns>Returns the latest version we can find.</returns>
        private async Task<Version> GetLatestVersionAsync2(int currentMajorVersion)
        {
            var versions = await GetReleaseVersions();

            Version latestVersion = versions.Where(v => v.Major == currentMajorVersion)
                                            .OrderByDescending(v => v)
                                            .FirstOrDefault() ?? new Version();

            return latestVersion;
        }

        /// <summary>
        /// Get all the release versions we can find on the Github page.
        /// Ignore prerelease versions.
        /// </summary>
        /// <returns>Returns a list of all non-prerelease versions found.</returns>
        private async Task<List<Version>> GetReleaseVersions()
        {
            var releasePage = await GetReleasesPageAsync();

            if (releasePage == null)
                return new List<Version>();

            var body = releasePage.DocumentNode.Element("html").Element("body");

            var appMain = body.GetChildWithClass("application-main");
            var repoContent = appMain?.GetDescendantWithClass("repository-content");
            var releaseEntries = repoContent?.GetDescendantsWithClass("release-entry");

            if (releaseEntries == null)
                return new List<Version>();

            List<Version> versions = new List<Version>();

            foreach (var entry in releaseEntries)
            {
                var (prerelease, version) = GetReleaseInfo(entry);

                if (!prerelease && Version.TryParse(version, out Version result))
                {
                    versions.Add(result);
                }
            }

            return versions;
        }

        /// <summary>
        /// Get the Github page that contains the latest releases.
        /// </summary>
        /// <returns>Returns the HTML document for the requested page,
        /// or null if it fails to load.</returns>
        private async Task<HtmlDocument?> GetReleasesPageAsync()
        {
            HtmlDocument? doc = await pageProvider.GetHtmlDocumentAsync(githubReleasesPage,
                "Github Releases", CachingMode.BypassCache, ShouldCache.No,
                SuppressNotifications.Yes, CancellationToken.None).ConfigureAwait(false);

            return doc;
        }

        /// <summary>
        /// Given a release-entry node from the Github page, extract the 
        /// prerelease status and version string.
        /// </summary>
        /// <param name="entry">A div containing release information.</param>
        /// <returns>Returns whether the entry contains a prerelease version,
        /// and what the version is.</returns>
        private (bool prerelease, string version) GetReleaseInfo(HtmlNode entry)
        {
            var prerelease = entry.GetDescendantWithClass("Label--prerelease");

            var ul = entry.Descendants("ul").FirstOrDefault();
            var titled = ul?.Element("li")?.Element("a");
            var title = titled?.GetAttributeValue("title", "") ?? "";

            return (prerelease != null, title);
        }
        #endregion
    }
}
