using BanchoNET.Core.Models.Api.Notifications;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Infrastructure.Controllers.Api;

public partial class ApiController
{
    [HttpGet("notifications")]
    public ActionResult<NotificationsResponse> GetNotifications() {
        if (!User.TryGetUserId(out var uid)) return Unauthorized();
        
        //TODO get notifications
        
        return JsonSnake(new NotificationsResponse());
    }
}