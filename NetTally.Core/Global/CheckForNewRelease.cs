using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using NetTally.Extensions;
using NetTally.SystemInfo;
using NetTally.Types.Enums;
using NetTally.Web;

namespace NetTally.Global
{
    public partial class CheckForNewRelease : ObservableObject, IDisposable
    {
        const string githubReleasesPage = "https://github.com/Kinematics/NetTally/releases";

        readonly IPageProvider pageProvider;
        readonly ILogger<CheckForNewRelease> logger;

        readonly Timer timer;
        readonly TimeSpan initialDelay = TimeSpan.FromSeconds(15);
        readonly TimeSpan periodDelay = TimeSpan.FromDays(1);

        const int frameworkVersion = 2;

        public CheckForNewRelease(IPageProvider provider, ILogger<CheckForNewRelease> logger)
        {
            pageProvider = provider;
            this.logger = logger;

            timer = new Timer(TimerCallback);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
            }
        }

        #region Timer
        public void Start()
        {
            timer.Change(initialDelay, periodDelay);
            logger.LogDebug("Timer started");
        }

        private async void TimerCallback(object? obj)
        {
            // Don't need to repeat if we already found a new version.
            if (HasNewRelease)
                return;

            try
            {
                bool newVersion = await DoVersionCheckAsync();

                if (newVersion)
                {
                    HasNewRelease = true;
                }
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Check for new release update attempt failed.");
            }
        }
        #endregion Timer


        [ObservableProperty]
        bool hasNewRelease = false;

        #region Private version checking methods
        /// <summary>
        /// Check to see if there's a newer release than the currently running version.
        /// If a newer version is found, the NewRelease property is set to true.
        /// If no newer version is found, it sets up a request to re-run this function in 2 days time.
        /// </summary>
        /// <returns>Returns nothing.  Just runs async.</returns>
        private async Task<bool> DoVersionCheckAsync()
        {
            Version currentVersion = ProductInfo.FileVersion;

            if (currentVersion == null)
                return false;

            Version latestVersion = await GetLatestVersionAsync();

            return latestVersion.CompareTo(currentVersion) > 0;
        }


        /// <summary>
        /// Get the latest version we can find that matches the provided
        /// current major version number.
        /// Version 2 and version 3 have different runtime requirements, so
        /// don't suggest upgrades across major versions.
        /// </summary>
        /// <returns>Returns the latest version we can find.</returns>
        private async Task<Version> GetLatestVersionAsync()
        {
            var versions = await GetReleaseVersionsAsync();

            Func<Version, bool> testForMajor;
            if (ProductInfo.FileVersion.Major == frameworkVersion)
                testForMajor = (v) => v.Major == frameworkVersion;
            else
                testForMajor = (v) => true;

            Version latestVersion = versions.Where(testForMajor)
                                            .OrderDescending()
                                            .FirstOrDefault(new Version());

            return latestVersion;
        }

        /// <summary>
        /// Get all the release versions we can find on the Github page.
        /// Ignore prerelease versions.
        /// </summary>
        /// <returns>Returns a list of all non-prerelease versions found.</returns>
        private async Task<List<Version>> GetReleaseVersionsAsync()
        {
            List<Version> versions = [];

            var releasePage = await GetReleasesPageAsync();

            if (releasePage is not null)
            {
                var body = releasePage.DocumentNode.Element("html").Element("body");

                var appMain = body.GetChildWithClass("application-main");
                var repoContent = appMain?.GetDescendantWithClass("repository-content");
                var releaseEntries = repoContent?.GetDescendantsWithClass("release-entry");

                if (releaseEntries is not null)
                {
                    foreach (var entry in releaseEntries)
                    {
                        var (prerelease, version) = GetReleaseInfo(entry);

                        if (!prerelease && Version.TryParse(version, out Version? result))
                        {
                            versions.Add(result);
                        }
                    }
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
        private static (bool prerelease, string version) GetReleaseInfo(HtmlNode entry)
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
