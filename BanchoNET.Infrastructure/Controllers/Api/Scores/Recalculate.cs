using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Scores;

public partial class ScoresController
{
    [HttpPost("recalculate")]
    public async Task<ActionResult> RecalculateAllScores() {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        var player = await Players.GetPlayer(uid);
        if (player == null || !((PlayerPrivileges)player.Privileges).HasPrivilege(PlayerPrivileges.Staff))
            return Unauthorized();
        
        await scores.RecalculateAllScores();
        return Ok();
    }

    [HttpPost("{scoreId:long}/recalculate")]
    public async Task<ActionResult> RecalculateScore(
        long scoreId
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var player = await Players.GetPlayer(uid);
        if (player == null || !((PlayerPrivileges)player.Privileges).HasPrivilege(PlayerPrivileges.Staff))
            return Unauthorized();

        return await scores.RecalculateScore(scoreId)
            ? Ok()
            : NotFound();
    }
}