using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Scores;

public partial class ScoresController
{
    [HttpGet("{id:long}/download")]
    public async Task<IActionResult> DownloadScore(
        long id
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();

        var score = await scores.GetScore(id);
        if (score is not { HasReplay: true })
            return NotFound();

        await Players.IncreasePlayerReplaysViewed(score.PlayerId, score.Mode, score.MapId);
        
        return new PhysicalFileResult(Storage.GetReplayPath(id), "application/x-osu-replay");
    }
}