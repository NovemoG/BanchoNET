using BanchoNET.Core.Abstractions.Bancho.Coordinators;
using BanchoNET.Core.Abstractions.Bancho.Services;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Packets;
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
        logger.LogDebug($"Logout time difference: {DateTime.Now - player.LoginTime}");
        if (DateTime.Now - player.LoginTime < TimeSpan.FromSeconds(1)) return false;

        if (player.InMatch) multiplayer.LeavePlayer(player);

        player.Spectating?.RemoveSpectator(player);

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
        return true;
    }
}