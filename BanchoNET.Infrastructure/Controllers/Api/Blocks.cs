using BanchoNET.Core.Attributes;
using BanchoNET.Core.Models.Auth;
using BanchoNET.Core.Models.Api.Relationships;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("blocks")]
    [RequireScope(OAuthScopes.FriendsRead)]
    public async Task<ActionResult<Relationship[]>> GetBlocks() {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();

        var blocks = await Players.GetPlayerBlocks(uid);
        var blockList = await PopulateRelationships(blocks, "block"); //TODO type

        return JsonSnake(blockList);
    }
}