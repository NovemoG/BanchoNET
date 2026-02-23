namespace BanchoNET.Middlewares;

public class SubdomainMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        var subdomain = host.Split('.', 2)[0];
        
        context.Items["subdomain"] = subdomain;

        await next(context);
    }
}