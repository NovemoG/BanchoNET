using BanchoNET.Objects.Players;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task TournamentLeaveMatchChannel(Player player, BinaryReader br)
    {
        var matchId = br.ReadInt32();
        
        if (matchId is < 0 or > short.MaxValue)
            return Task.CompletedTask;
        
        var match = session.GetLobby((ushort)matchId);
        if (match == null)
            return Task.CompletedTask;
        
        player.LeaveChannel(match.Chat);
        match.TourneyClients.Remove(player.Id);
        
        return Task.CompletedTask;
    }
}