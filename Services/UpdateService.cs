using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LatexStudio.Services;

public record GitHubRelease(
    [property: JsonPropertyName("tag_name")] string TagName,
    [property: JsonPropertyName("html_url")] string HtmlUrl
);

public sealed class UpdateService
{
    private const string RepoUrl = "https://api.github.com/repos/munanyo30/LatexStudio-Releases/releases/latest";
    private readonly HttpClient client;

    public UpdateService()
    {
        client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "LatexStudio-UpdateChecker");
    }

    public string CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public async Task<(bool UpdateAvailable, string NewVersion, string DownloadUrl)> CheckForUpdatesAsync()
    {
        try
        {
            var release = await client.GetFromJsonAsync<GitHubRelease>(RepoUrl);
            if (release == null) return (false, "", "");

            var latestVersion = release.TagName.TrimStart('v');
            var current = CurrentVersion;

            if (IsNewer(latestVersion, current))
            {
                return (true, latestVersion, release.HtmlUrl);
            }
        }
        catch
        {
            // Fail silently if no internet or repo private
        }
        return (false, "", "");
    }

    private static bool IsNewer(string latest, string current)
    {
        if (Version.TryParse(latest, out var vLatest) && Version.TryParse(current, out var vCurrent))
        {
            return vLatest > vCurrent;
        }
        return false;
    }
}
