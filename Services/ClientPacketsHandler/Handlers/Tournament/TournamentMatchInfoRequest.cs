using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Packets;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task TournamentMatchInfoRequest(Player player, BinaryReader br)
    {
        var matchId = br.ReadInt32();
        
        if (matchId is < 0 or > short.MaxValue)
            return Task.CompletedTask;

        if (!player.Privileges.HasPrivilege(Privileges.Supporter))
            return Task.CompletedTask;
        
        var match = session.GetLobby((ushort)matchId);
        if (match == null)
            return Task.CompletedTask;

        using var packet = new ServerPackets();
        packet.UpdateMatch(match, false);
        player.Enqueue(packet.GetContent());
        
        return Task.CompletedTask;
    }
}