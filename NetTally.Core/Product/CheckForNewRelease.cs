using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using NetTally.Enums;
using NetTally.Extensions;
using NetTally.Product;
using NetTally.Web;

namespace NetTally.Data
{
    public partial class CheckForNewRelease : ObservableObject, IDisposable
    {
        const string githubReleasesPage = "https://github.com/Kinematics/NetTally/releases";
        const string githubLatestPage = "https://github.com/Kinematics/NetTally/releases/latest";

        readonly IPageProvider pageProvider;
        readonly ILogger<CheckForNewRelease> logger;

        readonly Timer timer;
        readonly TimeSpan initialDelay = TimeSpan.FromSeconds(10);
        readonly TimeSpan periodDelay = TimeSpan.FromDays(1);

        const int frameworkVersion = 2;

        [ObservableProperty]
        bool hasNewRelease = false;

        readonly Regex TagVersionRegex = ReleasesTagRegex();

        [GeneratedRegex(@"releases/tag/v?(?<tag>.+)$")]
        private static partial Regex ReleasesTagRegex();


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

        #region Private version checking methods
        /// <summary>
        /// Check to see if there's a newer release than the currently running version.
        /// </summary>
        /// <returns><c>True</c> if there is a newer version available. Otherwise <c>false</c>.</returns>
        private async Task<bool> DoVersionCheckAsync()
        {
            Version currentVersion = ProductInfo.FileVersion;

            // If we can't load the program version, we can't do the check.
            if (currentVersion == null)
                return false;

            Version latestVersion = await GetLatestVersionAsync();

            return latestVersion > currentVersion;
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
            // Try to load the URL that should just return a redirection
            // with the latest version tag first.
            Version? redirectVersion = await GetLatestRedirectVersion();

            if (redirectVersion is not null)
            {
                return redirectVersion;
            }

            // Otherwise load the entire releases page and filter that.
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
        /// Load the "latest" release page headers and see if it redirects
        /// to a tagged page. If so, and that tag can be parsed as a version,
        /// return that version value.
        /// </summary>
        /// <returns>The latest version tag, if found. Otherwise <c>null</c>.</returns>
        private async Task<Version?> GetLatestRedirectVersion()
        {
            string redirectURL = await pageProvider.GetRedirectUrlAsync(
                githubLatestPage,
                "Latest release page",
                CachingMode.BypassCache,
                ShouldCache.No,
                SuppressNotifications.Yes,
                default);

            // Example redirect: https://github.com/Kinematics/NetTally/releases/tag/4.0.2

            Match m = TagVersionRegex.Match(redirectURL);
            if (m.Success)
            {
                string tag = m.Groups["tag"].Value;
                if (Version.TryParse(tag, out Version? result))
                {
                    return result;
                }
            }

            return null;
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
