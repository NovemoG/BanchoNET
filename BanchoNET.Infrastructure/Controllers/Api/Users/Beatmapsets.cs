using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Users;

public partial class UsersController
{
    [HttpGet("beatmapsets/{type}")]
    public async Task<ActionResult<List<ApiBeatmapset>>> GetBeatmapsets(
        int userId,
        string type
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        return JsonSnake(new List<ApiBeatmapset>());
    }
}