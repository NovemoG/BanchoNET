using BanchoNET.Core.Abstractions.Bancho.Coordinators;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;
using Novelog.Abstractions;

namespace BanchoNET.Infrastructure.Bancho.Coordinators;

public class PlayerCoordinator(
    ILogger logger,
    IPlayerService players,
    IChannelService channels,
    IMultiplayerCoordinator multiplayer
) : IPlayerCoordinator
{
    public bool LogoutPlayer(
        User player
    ) {
        logger.LogDebug($"Logout time difference: {DateTime.UtcNow - player.LoginTime}");
        if (DateTime.UtcNow - player.LoginTime < TimeSpan.FromSeconds(1)) return false;

        if (player.InMatch) multiplayer.LeavePlayer(player);

        if (player.Spectating != null)
            RemoveSpectator(player.Spectating, player);

        while (player.Channels.Count != 0)
            channels.LeavePlayer(player.Channels[0], player);

        if (!player.IsRestricted)
        {
            players.EnqueueToPlayers(new ServerPackets()
                .Logout(player.Id)
                .FinalizeAndGetContent());
        }
        
        channels.RemoveChannel($"#s_{player.Id}");
        players.RemovePlayer(player);
        player.Logout();
        return true;
    }

    public bool AddSpectator(
        User host,
        User target
    ) {
        var channelName = $"s_{host.Id}";
        var spectatorChannel = channels.GetChannel(channelName);

        if (spectatorChannel == null)
        {
            spectatorChannel = new Channel(channelName)
            {
                Description = $"{host.Username}'s spectator channel",
                AutoJoin = false,
                Instance = true
            };

            channels.JoinPlayer(spectatorChannel, host);
            channels.InsertChannel(spectatorChannel);
        }

        if (!channels.JoinPlayer(spectatorChannel, target))
        {
            logger.LogDebug($"{target.Username} failed to join {channelName}", nameof(PlayerExtensions));
            return false;
        }

        var bytes = new ServerPackets()
            .FellowSpectatorJoined(target.Id)
            .FinalizeAndGetContent();

        foreach (var spectator in host.Spectators)
        {
            spectator.Enqueue(bytes);
            
            target.Enqueue(new ServerPackets()
                .FellowSpectatorJoined(spectator.Id)
                .FinalizeAndGetContent());
        }
        
        host.Enqueue(new ServerPackets()
            .SpectatorJoined(target.Id)
            .FinalizeAndGetContent());

        host.AddSpectator(target);
        target.Spectating = host;
        
        logger.LogDebug($"{target.Username} is now spectating {host.Username}", nameof(PlayerExtensions));
        return true;
    }

    public void RemoveSpectator(
        User host,
        User target
    ) {
        host.RemoveSpectator(target);
        target.Spectating = null;
        
        var spectatorChannel = channels.GetChannel($"#s_{host.Id}")!; //TODO can this be null?
        channels.LeavePlayer(spectatorChannel, target);

        if (host.SpectatorsCount == 0)
        {
            channels.LeavePlayer(spectatorChannel, host);
            channels.RemoveChannel(spectatorChannel);
        }
        else
        {
            using var returnPacket = new ServerPackets()
                .ChannelInfo(spectatorChannel);
            
            target.Enqueue(returnPacket.GetContent());

            returnPacket.FellowSpectatorLeft(target.Id);

            var bytes = returnPacket.GetContent();
            foreach (var spectator in host.Spectators)
                spectator.Enqueue(bytes);
        }
        
        host.Enqueue(new ServerPackets()
            .SpectatorLeft(target.Id)
            .FinalizeAndGetContent());
        
        logger.LogDebug($"{target.Username} is no longer spectating {host.Username}", nameof(PlayerExtensions));
    }
}