using BanchoNET.Core.Models.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task TournamentLeaveMatchChannel(User player, BinaryReader br)
    {
        var matchId = br.ReadInt32();
        
        if (matchId is < 0 or > short.MaxValue)
            return Task.CompletedTask;
        
        var match = multiplayer.GetMatch((ushort)matchId);
        if (match == null)
            return Task.CompletedTask;

        channels.LeavePlayer(match.Chat, player);
        match.TourneyClients.Remove(player.Id);
        
        return Task.CompletedTask;
    }
}