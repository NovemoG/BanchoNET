using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmapsets;

public partial class BeatmapsetsController
{
    [HttpPost("{beatmapsetId:int}/refresh")]
    public async Task<ActionResult<ApiBeatmapsetFull>> RefreshBeatmapset(
        int beatmapsetId
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var player = await Players.GetPlayerOrOffline(uid);
        if (player == null) return NotFound();

        var lookupStart = DateTime.UtcNow;

        var beatmapset = await Beatmaps.GetBeatmapset(beatmapsetId);
        if (beatmapset == null) return NotFound();

        if (beatmapset.IsPrivateUpload)
            return BadRequest(new { error = "This beatmapset was uploaded here and doesn't exist on osu!." });

        // a set we didn't have yet was just fetched by the lookup above, so it's already up to date
        if (beatmapset.LastApiCheck < lookupStart)
        {
            if (!player.Privileges.HasPrivilegeOrHigher(PlayerPrivileges.Nominator))
            {
                var remaining = beatmapset.LastApiCheck + BeatmapExtensions.MinimumApiCheckInterval - DateTime.UtcNow;

                if (remaining > TimeSpan.Zero)
                    return StatusCode(StatusCodes.Status429TooManyRequests, new
                    {
                        error = $"This beatmapset was refreshed recently, try again in {FormatRemaining(remaining)}."
                    });
            }

            if (await Beatmaps.GetBeatmapset(beatmapsetId, recheckApi: true) == null)
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = "Couldn't fetch this beatmapset from osu!, try again later."
                });
        }

        var refreshed = await Beatmaps.GetBeatmapsetFromApiOrCached(beatmapsetId, withAllData: false);
        if (refreshed == null) return NotFound();

        await Beatmaps.FetchPlayerPlaycount(refreshed, uid);
        await Beatmaps.FetchPlayerFavorited(refreshed, uid);

        return JsonSnake(refreshed);
    }

    private static string FormatRemaining(
        TimeSpan remaining
    ) {
        if (remaining.TotalHours >= 1)
            return $"{(int)Math.Ceiling(remaining.TotalHours)} hours";

        return $"{Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))} minutes";
    }
}