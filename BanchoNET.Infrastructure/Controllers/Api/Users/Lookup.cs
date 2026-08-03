using BanchoNET.Core.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api.Users;

public partial class UsersController
{
    [HttpGet("lookup")]
    [AllowClientCredentials]
    public async Task<ActionResult> LookupUsers(
        [FromQuery(Name = "ids[]")] int[] playerIds
    ) {
        var players = await Players.GetPlayers(playerIds);

        return JsonSnake(new { users = players });
    }
}