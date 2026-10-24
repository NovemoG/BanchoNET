using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Beatmaps;

public partial class BeatmapsController
{
    [HttpPost("{beatmapId:int}/change-status")]
    public async Task<IActionResult> ChangeBeatmapStatus(
        int beatmapId,
        [FromForm] BeatmapStatus targetStatus
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        var player = await Players.GetPlayerOrOffline(uid);
        if (player == null) return NotFound();
        
        if (!player.Privileges.HasPrivilegeOrHigher(PlayerPrivileges.Nominator))
            return Unauthorized();

        var beatmap = await Beatmaps.GetBeatmap(beatmapId);
        if (beatmap == null) return NotFound();

        // unranking gives the beatmap its official status back
        if (targetStatus.HasLeaderboard())
            await beatmapsRepository.UpdateBeatmapStatus(beatmapId, targetStatus);
        else
            await Beatmaps.RestoreOfficialStatus(beatmap.Set.Id, beatmapId);
        
        return Ok();
    }
}