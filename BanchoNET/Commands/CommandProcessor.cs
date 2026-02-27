using BanchoNET.Core.Abstractions.Bancho.Coordinators;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Abstractions.Repositories;
using BanchoNET.Core.Abstractions.Repositories.Histories;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using static BanchoNET.Core.Utils.Maps.CommandHandlerMap;

namespace BanchoNET.Commands;

public partial class CommandProcessor(
    IPlayerService playerService,
    IPlayerCoordinator playerCoordinator,
    IMultiplayerCoordinator multiplayer,
    IChannelService channels,
    IScoresRepository scores,
    IPlayersRepository players,
    IBeatmapsRepository beatmaps,
    IBeatmapHandler beatmapHandler,
    IHistoriesRepository histories
) : ICommandProcessor
{
    private static readonly string Prefix = AppSettings.CommandPrefix;

    private User _playerCtx = null!;
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
        User player,
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