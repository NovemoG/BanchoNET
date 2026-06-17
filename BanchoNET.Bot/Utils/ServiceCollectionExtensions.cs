using System.Reflection;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.TextCommands.Parsing;
using DSharpPlus.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace BanchoNET.Bot.Utils;

public static class ServiceCollectionExtensions
{
    public static void AddDiscordBot(
        this IServiceCollection services,
        string token,
        ulong debugGuildId,
        DiscordIntents intents
    ) {
        services.AddDiscordClient(token, intents);
        
        services.AddCommandsExtension((serviceProvider, extension) =>
        {
            var commands = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    t.GetCustomAttributes(typeof(CommandAttribute), inherit: true).Length != 0
                    || t.GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    ).Any(m => m.GetCustomAttributes(typeof(CommandAttribute), inherit: true).Length != 0)
                );
            
            extension.AddCommands(commands);
            var textCommandProcessor = new TextCommandProcessor(new TextCommandConfiguration
            {
                PrefixResolver = new DefaultPrefixResolver(true, "?", "&").ResolvePrefixAsync,
            });

            extension.AddProcessor(textCommandProcessor);
        }, new CommandsConfiguration
        {
            RegisterDefaultCommandProcessors = true,
            DebugGuildId = debugGuildId,
        });
    }
}