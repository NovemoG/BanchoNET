using System.Diagnostics;
using BanchoNET.Utils.Extensions;

namespace BanchoNET.Middlewares;

public class RequestTimingMiddleware(ILogger logger, RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.ApplyHeaders();

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        await next(context);

        stopwatch.Stop();
        //TODO temporary, will replace after logger update
        logger.LogDebug($"{context.Request.Host}{context.Request.Path} | " +
                        $"Request took: {stopwatch.Elapsed.Seconds}s " +
                        $"{stopwatch.Elapsed.Milliseconds}ms " +
                        $"{stopwatch.Elapsed.Microseconds}μs",
            caller: $"{context.Request.Method} {context.Response.StatusCode}");
    }
}