using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task SpectateFrames(User player, BinaryReader br)
    {
        //TODO idk how would server-side validation work so I'm just gonna send raw frames back (for now)
        //var frameBundle = br.ReadSpectateFrames();
        var rawFrameData = br.ReadRawData();
        
        var bytes = new ServerPackets()
            .SpectateFrames(rawFrameData)
            .FinalizeAndGetContent();
        
        foreach (var spectator in player.Spectators)
            spectator.Enqueue(bytes);
        
        return Task.CompletedTask;
    }
}