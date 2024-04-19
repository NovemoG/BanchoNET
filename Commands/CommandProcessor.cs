using BanchoNET.Models;
using BanchoNET.Objects.Players;
using BanchoNET.Services;
using BanchoNET.Utils;
using static BanchoNET.Utils.CommandHandlerMap;

namespace BanchoNET.Commands;

public partial class CommandProcessor(ScoresRepository scores, PlayersRepository players)
{
    private readonly int _prefixLength = AppSettings.CommandPrefix.Length;
    private readonly string _commandNotFound =
        $"Command not found. Please use '{AppSettings.CommandPrefix}help' to see all available commands.";
    
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
        
        if (!CommandsMap.TryGetValue(commandValues[0].ToLower(), out var cm))
            return _commandNotFound;

        //If a player doesn't have required privileges to execute this command,
        //we don't want to expose the existence of this command to him
        if (!player.Privileges.HasAnyPrivilege(cm.Attribute.Privileges))
            return _commandNotFound;
        
        var returnValue = cm.Method.Invoke(this, [new CommandParameters
        {
            Player = player,
            CommandBase = commandValues[0]
        }, commandValues[1..]]);
        if (cm.Method.ReturnType != typeof(string) || returnValue == null)
            return "Some lazy ass developer forgot to make his command return a string value...";
        
        return (string)returnValue;
    }
}