using BanchoNET.Core.Attributes;
using BanchoNET.Core.Utils.Extensions;
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
        var online = await PlayerService.FilterOnline(players.Select(p => p.Id).ToArray());

        foreach (var player in players)
            player.IsOnline = online.Contains(player.Id);

        return JsonSnake(new { users = players });
    }
}