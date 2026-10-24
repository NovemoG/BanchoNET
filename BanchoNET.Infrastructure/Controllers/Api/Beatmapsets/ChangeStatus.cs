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

        if (!player.Privileges.HasPrivilegeOrHigher(PlayerPrivileges.Nominator))
            return Unauthorized();

        var beatmapset = await Beatmaps.GetBeatmapset(beatmapsetId);
        if (beatmapset == null) return NotFound();

        // unranking gives the set its official status back
        if (targetStatus.HasLeaderboard())
            await beatmapsRepository.UpdateBeatmapsetStatus(beatmapsetId, targetStatus);
        else
            await Beatmaps.RestoreOfficialStatus(beatmapsetId);

        return Ok();
    }

    [HttpPost("rank-all-pending")]
    public async Task<IActionResult> RankAllPending() {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var player = await Players.GetPlayerOrOffline(uid);
        if (player == null) return NotFound();

        if (!player.Privileges.HasPrivilegeOrHigher(PlayerPrivileges.Nominator))
            return Unauthorized();

        await beatmapsRepository.RankAllPending();

        return Ok();
    }
}