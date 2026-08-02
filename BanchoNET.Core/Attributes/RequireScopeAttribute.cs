using BanchoNET.Core.Models.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BanchoNET.Core.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireScopeAttribute(string scope) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Filters are ordered by scope, controller level before action level, so the last
        // one is the most specific and is the only one that should apply.
        var effective = context.Filters
            .OfType<RequireScopeAttribute>()
            .LastOrDefault();

        if (!ReferenceEquals(effective, this))
        {
            base.OnActionExecuting(context);
            return;
        }

        // Anonymous endpoints have no token to carry a scope
        var authenticated = context.HttpContext.User.Identity?.IsAuthenticated == true;

        if (authenticated && !OAuthScopes.Grants(context.HttpContext.User.FindFirst("scopes")?.Value, scope))
        {
            context.Result = new ObjectResult(new
            {
                error = "insufficient_scope",
                error_description = $"The request requires higher privileges than provided by the access token. Missing scope: {scope}",
                message = "Missing required scope."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

            return;
        }

        base.OnActionExecuting(context);
    }
}