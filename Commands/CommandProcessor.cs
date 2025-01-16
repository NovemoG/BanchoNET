using BanchoNET.Abstractions.Repositories;
using BanchoNET.Abstractions.Repositories.Histories;
using BanchoNET.Abstractions.Services;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Services;
using BanchoNET.Services.Repositories;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;
using static BanchoNET.Utils.Maps.CommandHandlerMap;

namespace BanchoNET.Commands;

public partial class CommandProcessor(
    IBanchoSession session,
    ScoresRepository scores,
    PlayersRepository players,
    IBeatmapsRepository beatmaps,
    IBeatmapHandler beatmapHandler,
    IHistoriesRepository histories
    ) : ICommandProcessor
{
    private static readonly string Prefix = AppSettings.CommandPrefix;

    private Player _playerCtx = null!;
    private Channel? _channelCtx;
    private string _commandBase = null!;

    /// <summary>
    /// Executes a command from a given string.
    /// </summary>
    /// <param name="command">Unedited string containing whole command</param>
    /// <param name="player">Player instance that used this command</param>
    /// <param name="channel">Target channel to which this command was sent</param>
    /// <returns>If <c>ToPlayer</c> is set to false, sends a message to everyone in
    /// chat instead of only to player</returns>
    public async Task<(bool ToPlayer, string Response)> Execute(
        string command,
        Player player,
        Channel? channel = null)
    {
        command = command[Prefix.Length..];
        
        var commandValues = command.Split(" ");
        
        if (!CommandsMap.TryGetValue(commandValues[0].ToLower(), out var cm))
            return (true, CommandNotFound);

        //If a player doesn't have required privileges to execute this command,
        //we don't want to expose the existence of this command to him
        if (!player.CanUseCommand(cm.Attribute.Privileges))
            return (true, CommandNotFound);
        
        _playerCtx = player;
        _channelCtx = channel;
        _commandBase = commandValues[0];
        
        if (cm.Method.ReturnType == typeof(Task<string>))
            return (true, await (Task<string>)cm.Method.Invoke(this, [commandValues[1..]])!);

        if (cm.Method.ReturnType == typeof(Task<(bool, string)>))
            return await (Task<(bool, string)>)cm.Method.Invoke(this, [commandValues[1..]])!;
        
        return (true, "Some lazy ass developer forgot to make his command return a string value...");
    }
}