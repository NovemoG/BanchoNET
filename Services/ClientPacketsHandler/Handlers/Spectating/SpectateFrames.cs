using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
    private Task SpectateFrames(Player player, BinaryReader br)
    {
        //TODO idk how would server-side validation work so I'm just gonna send raw frames back (for now)
        //var frameBundle = br.ReadSpectateFrames();
        var rawFrameData = br.ReadRawData();

        using var framesPacket = new ServerPackets();
        framesPacket.SpectateFrames(rawFrameData);
        var bytes = framesPacket.GetContent();
        
        foreach (var spectator in player.Spectators)
            spectator.Enqueue(bytes);
        
        return Task.CompletedTask;
    }
}