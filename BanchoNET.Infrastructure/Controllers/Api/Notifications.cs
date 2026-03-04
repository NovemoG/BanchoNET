using BanchoNET.Core.Models.Api.Notifications;
using BanchoNET.Core.Utils.Json;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("notifications")]
    public ActionResult<NotificationsResponse> GetNotifications() {
        if (!TryGetUserId(out var uid)) return Unauthorized();
        
        //TODO get notifications
        
        return new JsonResult(new NotificationsResponse(), SnakeCaseNamingPolicy.Options);
    }
}