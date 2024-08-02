using System.Diagnostics;
using System.Reflection;
using BanchoNET.Attributes;

namespace BanchoNET.Utils;

public static class CommandHandlerMap
{
    
    
    static CommandHandlerMap()
    {
        Console.WriteLine($"[CommandProcessor] Loaded commands in: {ReloadCommands()}");
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
        
        //TODO if commands ever have performance problems, we should consider splitting commands to different
        //     classes and create instances of them differently
        
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