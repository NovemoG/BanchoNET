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
            var isService = InheritsFromOpenGeneric(type, typeof(StatefulService<,>));
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

    private static bool InheritsFromOpenGeneric(
        Type type,
        Type openGenericType
    ) {
        var currentType = type.BaseType;
        while (currentType != null && currentType != typeof(object))
        {
            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == openGenericType)
            {
                return true;
            }
            currentType = currentType.BaseType;
        }
        return false;
    }
}