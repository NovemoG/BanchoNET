using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Users;

public partial class UsersController
{
    [HttpGet("kudosu")]
    public async Task<ActionResult<int[]>> GetKudosu(
        int userId,
        [FromQuery] int offset,
        [FromQuery] int limit
    ) {
        return Ok(Array.Empty<int>());
    }
}