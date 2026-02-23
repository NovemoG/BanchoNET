using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task TournamentMatchInfoRequest(Player player, BinaryReader br)
    {
        var matchId = br.ReadInt32();
        
        if (matchId is < 0 or > short.MaxValue)
            return Task.CompletedTask;

        if (!player.Privileges.HasPrivilege(PlayerPrivileges.Supporter))
            return Task.CompletedTask;
        
        var match = session.GetLobby((ushort)matchId);
        if (match == null)
            return Task.CompletedTask;
        
        player.Enqueue(new ServerPackets()
            .UpdateMatch(match, false)
            .FinalizeAndGetContent());
        
        return Task.CompletedTask;
    }
}