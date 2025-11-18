using System.Text.Json;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using NetTally.Enums;
using NetTally.Utility.HtmlNodes;
using NetTally.Utility.Json;
using NetTally.Web;

namespace NetTally.Product;

public partial class CheckForNewRelease : ObservableObject, IDisposable
{
    const string githubReleasesPage = "https://github.com/Kinematics/NetTally/releases";
    const string githubLatestPage = "https://github.com/Kinematics/NetTally/releases/latest";
    const string githubApiPage = "https://api.github.com/repos/Kinematics/NetTally/releases";

    readonly IPageProvider pageProvider;
    readonly ILogger<CheckForNewRelease> logger;

    readonly Timer timer;
    readonly TimeSpan initialDelay = TimeSpan.FromSeconds(10);
    readonly TimeSpan periodDelay = TimeSpan.FromDays(1);

    const int frameworkVersion = 2;

    [ObservableProperty]
    public partial bool HasNewRelease { get; set; } = false;

    [GeneratedRegex(@"releases/tag/v?(?<tag>.+)$")]
    private static partial Regex ReleasesTagRegex { get; }

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
#if DEBUG
        HasNewRelease = true;
#else
        timer.Change(initialDelay, periodDelay);
#endif
    }

    private async void TimerCallback(object? obj)
    {
        // Don't need to repeat if we already found a new version.
        if (HasNewRelease)
            return;

        try
        {
            HasNewRelease = await DoVersionCheckAsync();
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

        Version? latestVersion = await GetLatestVersionAsync();

        return latestVersion is not null && latestVersion > currentVersion;
    }

    /// <summary>
    /// Get the latest version we can find that matches the provided
    /// current major version number.
    /// Version 2 and version 3 have different runtime requirements, so
    /// don't suggest upgrades across major versions.
    /// </summary>
    /// <returns>Returns the latest version we can find.</returns>
    private async Task<Version?> GetLatestVersionAsync()
    {
        var latestVersion = await GetLatestVersionViaApi() ??
                            await GetLatestVersionViaRedirect() ??
                            await GetLatestVersionViaScrape();

        return latestVersion;
    }

    /// <summary>
    /// Try to read the JSON API page Github provides for the repo,
    /// and extract the latest version released.
    /// </summary>
    /// <returns>The latest <see cref="Version"/> it can find, or <c>null</c></returns>
    private async Task<Version?> GetLatestVersionViaApi()
    {
        var json = await pageProvider.GetJsonDocumentAsync(githubApiPage,
            "api", CachingMode.ReadWrite, SuppressNotifications.Yes, default);

        if (string.IsNullOrEmpty(json))
            return null;

        var releases = JsonSerializer.Deserialize(json,
            GithubSourceGeneratorContext.Default.ListGithubRelease);

        //var releases = JsonSerializer.Deserialize<List<GithubRelease>>(json, jsonOptions);

        if (releases is null)
            return null;

        var latestVersion = releases
            .Where(r => r.Version is not null)
            .Select(r => r.Version!)
            .Where(FilterForOldFramework)
            .OrderDescending()
            .FirstOrDefault();

        return latestVersion;
    }

    /// <summary>
    /// Try to load the URL that should just return a redirection
    /// with the latest version tag first.
    /// </summary>
    /// <returns>The latest <see cref="Version"/> it can find, or <c>null</c></returns>
    private async Task<Version?> GetLatestVersionViaRedirect()
    {
        // Try to load the URL that should just return a redirection
        // with the latest version tag first.
        Version? redirectVersion = await GetLatestRedirectVersion();

        return redirectVersion;
    }

    /// <summary>
    /// Try to load the entire releases page and filter that for
    /// the latest version information.
    /// </summary>
    /// <returns>The latest <see cref="Version"/> it can find, or <c>null</c></returns>
    private async Task<Version?> GetLatestVersionViaScrape()
    {
        var versions = await GetReleaseVersionsAsync();

        if (versions is null || versions.Count == 0)
            return null;

        var latestVersion = versions
            .Where(FilterForOldFramework)
            .OrderDescending()
            .FirstOrDefault();

        return latestVersion;
    }

    private static bool FilterForOldFramework(Version version)
    {
        if (ProductInfo.FileVersion.Major == frameworkVersion)
            return version.Major == frameworkVersion;
        else
            return true;
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
            SuppressNotifications.Yes,
            default);

        // Example redirect: https://github.com/Kinematics/NetTally/releases/tag/4.0.2

        Match m = ReleasesTagRegex.Match(redirectURL);
        if (m.Success)
        {
            string tag = m.Groups["tag"].Value;
            if (Version.TryParse(tag, out Version? result))
            {
                if (FilterForOldFramework(result))
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
            var body = releasePage.DocumentNode.Element("html")?.Element("body");

            var appMain = body?.GetChildWithClass("application-main");
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
            "Github Releases", CachingMode.NoCache,
            SuppressNotifications.Yes, CancellationToken.None);

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
