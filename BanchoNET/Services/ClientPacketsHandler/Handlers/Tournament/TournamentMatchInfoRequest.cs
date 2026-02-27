using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task TournamentMatchInfoRequest(User player, BinaryReader br)
    {
        var matchId = br.ReadInt32();
        if (matchId is < 0 or > short.MaxValue
            || !player.Privileges.HasPrivilege(PlayerPrivileges.Supporter))
        {
            return Task.CompletedTask;
        }

        var match = multiplayer.GetMatch((ushort)matchId);
        if (match == null)
            return Task.CompletedTask;
        
        player.Enqueue(new ServerPackets()
            .UpdateMatch(match, false)
            .FinalizeAndGetContent());
        
        return Task.CompletedTask;
    }
}