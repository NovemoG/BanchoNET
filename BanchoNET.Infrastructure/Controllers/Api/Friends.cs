using BanchoNET.Core.Models.Api.Relationships;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("friends")]
    public ActionResult<Relationship[]> GetFriends() {
        if (!TryGetUserId(out var uid)) return Unauthorized();
        
        //TODO get friends
        
        return new JsonResult(Array.Empty<Relationship>(), SnakeCaseNamingPolicy.Options);
    }
}