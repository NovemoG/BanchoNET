using BanchoNET.Core.Models.Api.Chat;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpPost("chat/ack")]
    public ActionResult<ChatAckResponse> ChatAck(
        [FromForm] ChatAckRequest request
    ) {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        //TODO get silenced players/messages?

        return JsonSnake(new ChatAckResponse());
    }
}