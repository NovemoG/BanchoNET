using System.Diagnostics;
using System.Reflection;
using BanchoNET.Attributes;
using BanchoNET.Objects.Players;
using BanchoNET.Services;
using BanchoNET.Utils;

namespace BanchoNET.Commands;

public partial class CommandProcessor
{
    private readonly BanchoHandler _bancho;
    
    public CommandProcessor(BanchoHandler bancho)
    {
        _bancho = bancho;
        
        Console.WriteLine($"[CommandProcessor] Loaded commands in: {ReloadCommands()}");
    }
    
    private readonly int _prefixLength = AppSettings.CommandPrefix.Length;
    private readonly string _commandNotFound =
        $"Command not found. Please use '{AppSettings.CommandPrefix}help' to see all available commands.";
    
    private readonly Dictionary<string, (MethodInfo Method, CommandAttribute Attribute)> _commandMethodsMap = new();
    
    //TODO support creating custom commands (idk what can be possible)

    /// <summary>
    /// Executes a command from a given string.
    /// </summary>
    /// <param name="command">Unedited string containing whole command</param>
    /// <param name="player">Player instance that used this command</param>
    public string Execute(string command, Player player)
    {
        command = command[_prefixLength..];
        
        var commandValues = command.Split(" ");
        
        if (!_commandMethodsMap.TryGetValue(commandValues[0].ToLower(), out var cm))
            return _commandNotFound;

        //If a player doesn't have required privileges to execute this command,
        //we don't want to expose the existence of this command to him
        if (!player.Privileges.HasAnyPrivilege(cm.Attribute.Privileges))
            return _commandNotFound;

        var returnValue = cm.Method.Invoke(this, [player, commandValues[0], commandValues[1..]]);
        if (cm.Method.ReturnType != typeof(string) || returnValue == null)
            return "Some lazy ass developer forgot to make his command return a string value...";
        
        return (string)returnValue;
    }

    private TimeSpan ReloadCommands()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        _commandMethodsMap.Clear();
        
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        var methods = typeof(Program).Assembly
            .GetTypes()
            .SelectMany(t => t.GetMethods(flags))
            .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0);
        
        foreach (var method in methods)
        {
            var commandAttribute = (CommandAttribute)method.GetCustomAttributes(typeof(CommandAttribute), false)[0];
            
            _commandMethodsMap.Add(commandAttribute.Name.ToLower(), (method, commandAttribute));
            
            if (commandAttribute.Aliases == null)
                continue;
            
            foreach (var alias in commandAttribute.Aliases)
                _commandMethodsMap.Add(alias.ToLower(), (method, commandAttribute));
        }
        
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }
}