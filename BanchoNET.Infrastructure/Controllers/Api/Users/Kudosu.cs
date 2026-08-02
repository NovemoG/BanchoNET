using BanchoNET.Core.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Users;

public partial class UsersController
{
    [HttpGet("{userId:int}/kudosu")]
    [AllowClientCredentials]
    public async Task<ActionResult<int[]>> GetKudosu(
        int userId,
        [FromQuery] int offset,
        [FromQuery] int limit
    ) {
        return JsonSnake(Array.Empty<int>());
    }
}