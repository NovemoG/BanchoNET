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
        
        if (!player.Privileges.HasPrivilege(PlayerPrivileges.Nominator))
            return Unauthorized();

        var beatmap = await Beatmaps.GetBeatmap(beatmapId);
        if (beatmap == null) return NotFound();

        if (await beatmapsRepository.UpdateBeatmapStatus(beatmapId, targetStatus) > 0)
        {
            beatmap.Status = targetStatus;
            await scores.SetBeatmapScoresRankedStatus(beatmap.Id, ranked: true);
        }
        
        return Ok();
    }
}