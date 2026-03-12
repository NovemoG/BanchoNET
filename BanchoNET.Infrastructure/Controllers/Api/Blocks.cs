using BanchoNET.Core.Models.Api.Relationships;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("blocks")]
    public async Task<ActionResult<Relationship[]>> GetBlocks() {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var blocks = await Players.GetPlayerBlocks(uid);
        var blockList = PopulateRelationships(blocks, "block"); //TODO type
        
        return new JsonResult(blockList, SnakeCaseNamingPolicy.Options);
    }
}