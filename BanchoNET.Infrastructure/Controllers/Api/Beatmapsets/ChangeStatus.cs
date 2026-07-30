using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmapsets;

public partial class BeatmapsetsController
{
    [HttpPost("{beatmapsetId:int}/change-status")]
    public async Task<IActionResult> ChangeBeatmapsetStatus(
        int beatmapsetId,
        [FromForm] BeatmapStatus targetStatus
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var player = await Players.GetPlayerOrOffline(uid);
        if (player == null) return NotFound();

        if (!player.Privileges.HasPrivilege(PlayerPrivileges.Nominator))
            return Unauthorized();

        var beatmapset = await Beatmaps.GetBeatmapset(beatmapsetId);
        if (beatmapset == null) return NotFound();

        if (await beatmapsRepository.UpdateBeatmapsetStatus(beatmapsetId, targetStatus) > 0)
        {
            beatmapset.Status = targetStatus;
            foreach (var beatmap in beatmapset.Beatmaps)
            {
                beatmap.Status = targetStatus;
                await scores.SetBeatmapScoresRankedStatus(beatmap.Id, ranked: true);
            }
        }

        return Ok();
    }

    [HttpPost("rank-all-pending")]
    public async Task<IActionResult> RankAllPending() {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var player = await Players.GetPlayerOrOffline(uid);
        if (player == null) return NotFound();

        if (!player.Privileges.HasPrivilege(PlayerPrivileges.Nominator))
            return Unauthorized();

        var affected = await beatmapsRepository.RankAllPending();

        foreach (var setId in affected)
        {
            var beatmapset = await Beatmaps.GetBeatmapset(setId);
            if (beatmapset == null) continue;

            beatmapset.Status = BeatmapStatus.Ranked;
            foreach (var beatmap in beatmapset.Beatmaps)
            {
                beatmap.Status = BeatmapStatus.Ranked;
                await scores.SetBeatmapScoresRankedStatus(beatmap.Id, ranked: true);
            }
        }

        return Ok();
    }
}