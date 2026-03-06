using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Users;

public partial class UsersController
{
    [HttpGet("recent_activity")]
    public async Task<ActionResult<int[]>> GetRecentActivity(
        int userId,
        [FromQuery] int offset,
        [FromQuery] int limit
    ) {
        return Ok(Array.Empty<int>());
    }
}