namespace BanchoNET.Utils;

public class SubdomainMiddleware
{
    private readonly RequestDelegate _next;

    public SubdomainMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        var subdomain = host.Split('.')[0];
        
        context.Items["subdomain"] = subdomain;

        await _next(context);
    }
}