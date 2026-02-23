using System.Diagnostics;
using System.Reflection;
using BanchoNET.Attributes;

namespace BanchoNET.Utils.Maps;

public static class CommandHandlerMap
{
    public const string PlayerNotFound = "Player not found. Make sure you provided correct username.";
    public const string BeatmapNotFound = "Beatmap not found.";
    public static readonly string CommandNotFound =
        $"Command not found. Please use '{AppSettings.CommandPrefix}help' to see all available commands.";
    
    public static readonly string[] ValidPrivileges = ["nominator", "submitter", "moderator", "administrator", "developer"];
    public static readonly string[] ValidStatuses = ["love", "qualify", "approve", "rank", "unrank"];
    public static readonly string[] FreemodAliases = ["fm", "freemod", "freemods"];
    
    static CommandHandlerMap()
    {
        Logger.Shared.LogDebug($"Loaded commands in: {ReloadCommands()}", nameof(CommandHandlerMap));
    }
    
    public static readonly Dictionary<string, (MethodInfo Method, CommandAttribute Attribute)> CommandsMap = new();
    
    public static TimeSpan ReloadCommands()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        CommandsMap.Clear();
        
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        
        var methods = typeof(Program).Assembly
            .GetTypes()
            .SelectMany(t => t.GetMethods(flags))
            .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0);
        
        foreach (var method in methods)
        {
            var commandAttribute = (CommandAttribute)method.GetCustomAttributes(typeof(CommandAttribute), false)[0];
            
            CommandsMap.Add(commandAttribute.Name.ToLower(), (method, commandAttribute));
            
            if (commandAttribute.Aliases == null)
                continue;
            
            foreach (var alias in commandAttribute.Aliases)
                CommandsMap.Add(alias.ToLower(), (method, commandAttribute));
        }
        
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }
}