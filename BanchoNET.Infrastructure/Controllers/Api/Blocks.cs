using BanchoNET.Core.Models.Api.Relationships;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("blocks")]
    public ActionResult<Relationship[]> GetBlocks() {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        //TODO get blocks
        
        return new JsonResult(Array.Empty<Relationship>(), SnakeCaseNamingPolicy.Options);
    }
}