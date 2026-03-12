using BanchoNET.Core.Models.Api.Relationships;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("friends")]
    public async Task<ActionResult<Relationship[]>> GetFriends() {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        var friends = await Players.GetPlayerBlocks(uid);
        var friendList = PopulateRelationships(friends, "friend");
        
        return new JsonResult(friendList, SnakeCaseNamingPolicy.Options);
    }
}