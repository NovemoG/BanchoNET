using System.Diagnostics;
using BanchoNET.Core.Utils.Extensions;

// ReSharper disable ExplicitCallerInfoArgument

namespace BanchoNET.Middlewares;

public class RequestTimingMiddleware(ILogger<RequestTimingMiddleware> logger, RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.ApplyHeaders();

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        await next(context);

        stopwatch.Stop();
        logger.LogDebug($"{context.Request.Host}{context.Request.Path} | " +
                        $"Request took: {stopwatch.Elapsed.Seconds}s " +
                        $"{stopwatch.Elapsed.Milliseconds}ms " +
                        $"{stopwatch.Elapsed.Microseconds}μs",
            caller: context.Request.Method,
            atLine: context.Response.StatusCode);
    }
}