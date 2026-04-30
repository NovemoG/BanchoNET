using BanchoNET.Core.Models.Api.Relationships;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("friends")]
    public async Task<ActionResult<Relationship[]>> GetFriends() {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        var friends = await Players.GetPlayerFriends(uid);
        var friendList = PopulateRelationships(friends, "friend");

        return JsonSnake(friendList);
    }

    [HttpPost("friends")]
    public async Task<ActionResult<AddFriendResponse>> AddFriend(
        [FromQuery] int target
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        if (uid == target) return BadRequest();
        
        var mutual = await Players.AddRelation(uid, target, (byte)Relations.Friend);
        
        return JsonSnake(new { user_relation = new AddFriendResponse
        {
            TargetId = target,
            Mutual = mutual,
            RelationType = nameof(Relations.Friend).ToLower()
        }});
    }

    [HttpDelete("friends/{target:int}")]
    public async Task RemoveFriend(
        int target
    ) {
        if (!User.TryGetUserId(out var uid)) return;
        if (uid == target) return;

        await Players.RemoveRelation(uid, target, (byte)Relations.Friend);
    }
}