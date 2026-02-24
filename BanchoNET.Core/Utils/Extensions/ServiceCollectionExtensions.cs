using System.Reflection;
using BanchoNET.Core.Abstractions;
using BanchoNET.Core.Abstractions.Bancho.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BanchoNET.Core.Utils.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSessionServices(
        this IServiceCollection services,
        Assembly[] assemblies
    ) {
        var types = assemblies.SelectMany(a => a.GetTypes()).Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var type in types)
        {
            var isService = type.BaseType is { IsGenericType: true }
                            && type.BaseType.GetGenericTypeDefinition() == typeof(StatefulService<,>);

            var isCoordinator = typeof(ICoordinator).IsAssignableFrom(type);

            if (!isService && !isCoordinator) continue;
            
            var matchingInterface = type.GetInterfaces().FirstOrDefault(i => i.Name == $"I{type.Name}");
            if (matchingInterface is not null)
            {
                services.AddSingleton(matchingInterface, type);
            }
        }

        return services;
    }
}