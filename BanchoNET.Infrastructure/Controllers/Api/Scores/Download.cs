using BanchoNET.Core.Attributes;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Scores;

public partial class ScoresController
{
    [HttpGet("{id:long}/download")]
    [AllowClientCredentials]
    public async Task<IActionResult> DownloadScore(
        long id
    ) {
        var score = await scores.GetScore(id);
        if (score is not { HasReplay: true })
            return NotFound();
        
        User.TryGetUserId(out var uid);
        
        if (score.PlayerId != uid)
            await Players.IncreasePlayerReplaysViewed(score.PlayerId, (byte)score.Mode, score.Id);
        
        return new PhysicalFileResult(Storage.GetReplayPath(id), "application/x-osu-replay");
    }
}