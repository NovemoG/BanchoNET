using BanchoNET.Core.Models.Api.Chat;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpPost("chat/ack")]
    public ActionResult<ChatAckResponse> ChatAck() { //TODO what is the Form model? 
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        //TODO get silenced players?
        
        return new JsonResult(new ChatAckResponse(), SnakeCaseNamingPolicy.Options);
    }
}