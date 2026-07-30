using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api.Player;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Users;

public partial class UsersController
{
    [HttpGet("{userId:int}/recent_activity")]
    [AllowClientCredentials]
    public async Task<ActionResult<Activity[]>> GetRecentActivity(
        int userId,
        [FromQuery] int offset,
        [FromQuery] int limit
    ) {
        return JsonSnake(Array.Empty<Activity>());
    }
}