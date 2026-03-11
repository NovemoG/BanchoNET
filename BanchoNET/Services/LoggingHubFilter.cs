using Microsoft.AspNetCore.SignalR;

namespace BanchoNET.Services;

public class LoggingHubFilter(ILogger logger) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next
    ) {
        var methodName = invocationContext.HubMethodName;
        var methodInfo = invocationContext.HubMethod;
        var argList = invocationContext.HubMethodArguments;

        var paramTypes = methodInfo.GetParameters()
            .Select(p => p.ParameterType.FullName)
            .ToArray();

        var hubName = invocationContext.Hub?.GetType().FullName ?? "(unknown hub)";
        logger.LogInfo(
            $"SignalR call -> Hub: {hubName}, Method: {methodName}, ParamTypes: {paramTypes}, Args: {argList}"
        );

        var result = await next(invocationContext);
        return result;
    }
}