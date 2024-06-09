using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BanchoNET.Attributes;

public class SubdomainAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string[] _allowedSubdomain;

    public SubdomainAuthorizeAttribute(string[] allowedSubdomain)
    {
        _allowedSubdomain = allowedSubdomain;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var subdomain = context.HttpContext.Items["subdomain"]?.ToString();

        if (!_allowedSubdomain.Contains(subdomain))
            context.Result = new NotFoundResult();
        
        base.OnActionExecuting(context);
    }
}