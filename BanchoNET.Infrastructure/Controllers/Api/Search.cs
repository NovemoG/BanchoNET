using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("search")]
    public async Task<ActionResult<SearchResultResponse>> SearchQuery(
        [FromQuery] string mode,
        [FromQuery] string query
    ) {
        if (!User.TryGetUserId(out _)) return Unauthorized();

        //TODO
        switch (mode)
        {
            case "user":
                var results = await Players.GetPlayersFromQuery(query);

                foreach (var player in results)
                    PlayerService.IsOnline(player.Id);
                
                return JsonSnake(new { user = new SearchResultResponse
                {
                    Data = results,
                    Total = results.Count
                }});
            
            default:
                return BadRequest();
        }
    }
}