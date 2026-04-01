using System.Text.Json;

namespace BanchoNET.Core.Models;

public sealed record GitHubRelease(string TagName, bool Draft, bool Prerelease)
{
    public static GitHubRelease FromJson(JsonElement e)
    {
        return new GitHubRelease(
            TagName: e.GetProperty("tag_name").GetString() ?? "",
            Draft: e.GetProperty("draft").GetBoolean(),
            Prerelease: e.GetProperty("prerelease").GetBoolean());
    }
}