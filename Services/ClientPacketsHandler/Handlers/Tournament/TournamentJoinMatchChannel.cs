using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Packets;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task TournamentJoinMatchChannel(Player player, BinaryReader br)
    {
        var matchId = br.ReadInt32();
        
        if (matchId is < 0 or > short.MaxValue)
            return Task.CompletedTask;

        if (!player.Privileges.HasPrivilege(Privileges.Supporter))
            return Task.CompletedTask;
        
        var match = _session.GetLobby((ushort)matchId);
        if (match == null)
            return Task.CompletedTask;

        if (match.GetPlayerSlot(player) != null)
            return Task.CompletedTask;

        if (player.JoinChannel(match.Chat))
            match.TourneyClients.Add(player.Id);
            
        return Task.CompletedTask;
    }
}