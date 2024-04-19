using System.Diagnostics;
using System.Reflection;
using BanchoNET.Attributes;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Commands;

public static class CommandProcessor
{
    static CommandProcessor()
    {
        Console.WriteLine($"[CommandProcessor] Loaded commands in: {ReloadCommands()}");
    }
    
    private static readonly int PrefixLength = AppSettings.CommandPrefix.Length;
    public static readonly Dictionary<string, (MethodInfo Method, CommandAttribute Attribute)> CommandMethodsMap = new();
    
    //TODO support creating custom commands (idk what can be possible)

    /// <summary>
    /// Executes a command from a given string.
    /// </summary>
    /// <param name="command">Unedited string containing whole command</param>
    /// <param name="player">Player instance that used this command</param>
    public static string Execute(string command, Player player)
    {
        command = command[PrefixLength..];
        
        var commandValues = command.Split(" ");

        if (!CommandMethodsMap.TryGetValue(commandValues[0].ToLower(), out var cm))
        {
            return !player.Privileges.HasAnyPrivilege(cm.Attribute.Privileges)
                ? "" //if a player does not have privileges to use this command,
                     //we don't want to show him that command even exists
                : $"Command not found. Please use '{AppSettings.CommandPrefix}help' to see all available commands.";
        }

        var returnValue = cm.Method.Invoke(null, [player, commandValues[0], commandValues[1..]]);
        if (cm.Method.ReturnType != typeof(string) || returnValue == null)
            return "Some lazy ass developer forgot to make his command return a string value...";
        
        return (string)returnValue;
    }

    public static TimeSpan ReloadCommands()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        CommandMethodsMap.Clear();
        
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        var methods = typeof(Program).Assembly
            .GetTypes()
            .SelectMany(t => t.GetMethods(flags))
            .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0);
        
        foreach (var method in methods)
        {
            var commandAttribute = (CommandAttribute)method.GetCustomAttributes(typeof(CommandAttribute), false)[0];
            
            CommandMethodsMap.Add(commandAttribute.Name.ToLower(), (method, commandAttribute));
            
            if (commandAttribute.Aliases == null)
                continue;
            
            foreach (var alias in commandAttribute.Aliases)
                CommandMethodsMap.Add(alias.ToLower(), (method, commandAttribute));
        }
        
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }
}