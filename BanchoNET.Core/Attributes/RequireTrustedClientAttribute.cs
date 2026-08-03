using BanchoNET.Core.Abstractions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace BanchoNET.Core.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireTrustedClientAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    ) {
        var clientId = context.HttpContext.User.FindFirst("aud")?.Value;
        var auth = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();

        if (!await auth.IsClientTrusted(clientId))
        {
            context.Result = new ObjectResult(new
            {
                error = "insufficient_scope",
                error_description = "This endpoint is only available to first party clients.",
                message = "This endpoint is only available to first party clients."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

            return;
        }

        await next();
    }
}