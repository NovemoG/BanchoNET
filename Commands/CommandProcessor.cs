using System.Diagnostics;
using System.Reflection;
using BanchoNET.Attributes;
using BanchoNET.Utils;

namespace BanchoNET.Commands;

public static class CommandProcessor
{
    static CommandProcessor()
    {
        Console.WriteLine($"[CommandProcessor] Reloaded commands in: {ReloadCommands()}");
    }
    
    private static readonly int PrefixLength = AppSettings.CommandPrefix.Length;
    private static readonly Dictionary<string, MethodInfo> CommandMethodsMap = new();

    public static readonly Dictionary<string, (string Brief, string Detailed)> CommandDescriptions = [];
    
    //TODO support creating custom commands (idk what can be possible)

    /// <summary>
    /// Executes a command from a given string.
    /// </summary>
    /// <param name="command">Unedited string containing whole command</param>
    public static string Execute(string command)
    {
        command = command[PrefixLength..];
        
        var commandValues = command.Split(" ");
        
        if (CommandMethodsMap.TryGetValue(commandValues[0].ToLower(), out var method))
            return (string)method.Invoke(null, [commandValues[1..]])!;
        
        return $"Command not found. Please use {AppSettings.CommandPrefix}help to see all available commands.";
    }

    public static TimeSpan ReloadCommands()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        CommandMethodsMap.Clear();
        CommandDescriptions.Clear();
        
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        var methods = typeof(Program).Assembly
            .GetTypes()
            .SelectMany(t => t.GetMethods(flags))
            .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0);
        
        foreach (var method in methods)
        {
            var commandAttribute = (CommandAttribute)method.GetCustomAttributes(typeof(CommandAttribute), false)[0];
            var commandDescription = (commandAttribute.Name, commandAttribute.BriefDescription, commandAttribute.DetailedDescription);
            
            CommandMethodsMap.Add(commandAttribute.Name.ToLower(), method);
            
            if (commandAttribute.Aliases == null)
            {
                CommandDescriptions.Add(
                    commandDescription.Name.ToLower(),
                    (commandDescription.BriefDescription,
                     commandAttribute.DetailedDescription));
                continue;
            }

            var aliases = "";
            foreach (var alias in commandAttribute.Aliases)
            {
                var aliasValue = alias.ToLower();
                
                CommandMethodsMap.Add(aliasValue, method);
                aliases += aliasValue + ", ";
            }
            
            CommandDescriptions.Add(
                commandDescription.Name.ToLower(),
                (commandDescription.BriefDescription,
                 commandAttribute.DetailedDescription + "\nAliases/shortcuts: " + aliases[..^2]));
        }
        
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }
}