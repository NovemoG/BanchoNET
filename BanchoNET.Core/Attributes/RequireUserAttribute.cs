using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BanchoNET.Core.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireUserAttribute(bool required = true) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Filters are ordered by scope, controller level before action level, so the last
        // one is the most specific and is the only one that should apply.
        var effective = context.Filters
            .OfType<RequireUserAttribute>()
            .LastOrDefault();

        if (!ReferenceEquals(effective, this) || !required)
        {
            base.OnActionExecuting(context);
            return;
        }

        var authenticated = context.HttpContext.User.Identity?.IsAuthenticated == true;

        if (authenticated && !context.HttpContext.User.TryGetUserId(out _))
        {
            context.Result = new ObjectResult(new
            {
                error = "invalid_token",
                error_description = "This endpoint requires a token issued to a user.",
                message = "This endpoint requires a token issued to a user."
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };

            return;
        }

        base.OnActionExecuting(context);
    }
}

/// <summary>
/// Opts an action out of the <see cref="RequireUserAttribute"/> applied by its controller, letting a
/// client credentials token through. Only valid on actions that never touch the caller's identity —
/// such a token has no subject, so <c>TryGetUserId</c> always fails behind it.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AllowClientCredentialsAttribute() : RequireUserAttribute(required: false);