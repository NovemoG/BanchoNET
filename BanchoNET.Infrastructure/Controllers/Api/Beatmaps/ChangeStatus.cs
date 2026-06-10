using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmaps;

public partial class BeatmapsController
{
    [HttpPost("~/api/v2/beatmapsets/{beatmapsetId:int}/change-status")]
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

        await beatmapsRepository.UpdateBeatmapsetStatus(beatmapsetId, targetStatus);
        return Ok();
    }

    [HttpPost("change-status")]
    public async Task<IActionResult> ChangeBeatmapStatus(
        int beatmapId,
        [FromForm] BeatmapStatus targetStatus
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        var player = await Players.GetPlayerOrOffline(uid);
        if (player == null) return NotFound();
        
        if (!player.Privileges.HasPrivilege(PlayerPrivileges.Nominator))
            return Unauthorized();

        var beatmapset = await Beatmaps.GetBeatmap(beatmapId);
        if (beatmapset == null) return NotFound();

        await beatmapsRepository.UpdateBeatmapStatus(beatmapId, targetStatus);
        return Ok();
    }
}