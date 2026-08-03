using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("search")]
    [AllowClientCredentials]
    public async Task<ActionResult<SearchResultResponse>> SearchQuery(
        [FromQuery] string mode,
        [FromQuery] string query
    ) {
        //TODO
        switch (mode)
        {
            case "user":
                var results = await Players.GetPlayersFromQuery(query);

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