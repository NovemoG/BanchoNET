using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BanchoNET.Core.Attributes;

public class SubdomainAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string[] _allowedSubdomains;

    public SubdomainAuthorizeAttribute(params string[] allowedSubdomains)
    {
        _allowedSubdomains = allowedSubdomains;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var subdomain = context.HttpContext.Items["subdomain"]?.ToString();

        if (!_allowedSubdomains.Contains(subdomain))
            context.Result = new NotFoundResult();
        
        base.OnActionExecuting(context);
    }
}