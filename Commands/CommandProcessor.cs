using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Services.Repositories;
using BanchoNET.Utils;
using static BanchoNET.Utils.CommandHandlerMap;

namespace BanchoNET.Commands;

public partial class CommandProcessor(
    ScoresRepository scores,
    PlayersRepository players,
    BeatmapsRepository beatmaps,
    MultiplayerRepository multiplayer)
{
    private readonly string _prefix = AppSettings.CommandPrefix;
    private readonly int _prefixLength = AppSettings.CommandPrefix.Length;
    private readonly string _commandNotFound =
        $"Command not found. Please use '{AppSettings.CommandPrefix}help' to see all available commands.";

    private Player _playerCtx = null!;
    private Channel? _channelCtx;
    private string _commandBase = null!;
    
    //TODO support creating custom commands (idk what can be possible)

    /// <summary>
    /// Executes a command from a given string.
    /// </summary>
    /// <param name="command">Unedited string containing whole command</param>
    /// <param name="player">Player instance that used this command</param>
    /// <param name="channel">Target channel to which this command was sent</param>
    public async Task<string> Execute(string command, Player player, Channel? channel = null)
    {
        command = command[_prefixLength..];
        
        var commandValues = command.Split(" ");
        
        if (!CommandsMap.TryGetValue(commandValues[0].ToLower(), out var cm))
            return _commandNotFound;

        //If a player doesn't have required privileges to execute this command,
        //we don't want to expose the existence of this command to him
        if (!player.CanUseCommand(cm.Attribute.Privileges))
            return _commandNotFound;
        
        _playerCtx = player;
        _channelCtx = channel;
        _commandBase = commandValues[0];

        if (cm.Method.ReturnType != typeof(Task<string>))
            return "Some lazy ass developer forgot to make his command return a string value...";

        var returnValue = await (Task<string>)cm.Method.Invoke(this, [commandValues[1..]])!;
        
        return returnValue;
    }
}