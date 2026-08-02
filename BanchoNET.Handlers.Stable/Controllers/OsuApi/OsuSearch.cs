using System.Globalization;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Handlers.Stable.Controllers.OsuApi;

public partial class OsuController
{
    private const int PageSize = 100;
    
    [HttpGet("osu-search.php")]
    public async Task<IActionResult> OsuSearch(
        [FromQuery(Name = "u")] string username,
        [FromQuery(Name = "h")] string passwordMD5,
        [FromQuery(Name = "r")] int rankedStatus,
        [FromQuery(Name = "q")] string query,
        [FromQuery(Name = "m")] int mode,
        [FromQuery(Name = "p")] int pageNumber
    ) {
        var player = await players.GetPlayerFromLogin(username, passwordMD5);
        if (player == null)
            return Unauthorized("auth fail");
        
        var status = rankedStatus.ToApiFromDirect();
        string? sort;
        
        switch (query)
        {
            case "Newest":
                sort = status == 1 ? "ranked_desc" : "updated_desc";
                query = "";
                break;
            case "Top Rated":
                sort = "rating_desc";
                query = "";
                break;
            case "Most Played":
                sort = "plays_desc";
                query = "";
                break;
            default:
                sort = status == 1 ? "ranked_desc" : "updated_desc";
                break;
        }
        
        var (beatmapsets, _, _) = await beatmapSearch.SearchAsync(
            query,
            mode == -1 ? null : mode.ToString(),
            category: null,
            status.ToString(),
            genre: null,
            language: null,
            extra: null,
            rankAchieved: null,
            sort,
            rankedStatus == 7 ? "played" : null,
            nsfw: true,
            cursor: null,
            playerId: player.Id,
            count: PageSize,
            skip: pageNumber * PageSize
        );
        
        var returnResponse = new List<string>
        {
            $"{(beatmapsets.Count == PageSize ? 101 : beatmapsets.Count)}"
        };

        foreach (var beatmapset in beatmapsets.Where(beatmapset => beatmapset.Beatmaps.Count != 0))
        {
            beatmapset.Beatmaps.Sort((a, b) => a.DifficultyRating.CompareTo(b.DifficultyRating));

            var beatmapsString = beatmapset.Beatmaps.Aggregate("",
                (current, map) => current + $"{map.Version.Replace('|', 'I')} [{map.DifficultyRating.ToString("0.##", CultureInfo.InvariantCulture)}⭐]@{map.Mode},")[..^1];
            
            returnResponse.Add($"{beatmapset.Id}.osz|{beatmapset.Artist}|{beatmapset.Title}|{beatmapset.Creator}|{((BeatmapStatus)beatmapset.Ranked).ToLegacyStatus()}|{beatmapset.Rating.ToString("0.###", CultureInfo.InvariantCulture)}|{beatmapset.LastUpdated}|{beatmapset.Id}|0|{beatmapset.Video}|0|0|0|{beatmapsString}");
        }
        
        return Responses.BytesContentResult(string.Join("\n", returnResponse));
    }
}