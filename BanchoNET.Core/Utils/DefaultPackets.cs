using BanchoNET.Core.Packets;

namespace BanchoNET.Core.Utils;

public static class DefaultPackets
{
    public static byte[] MatchJoinFailData() =>
        new ServerPackets()
            .MatchJoinFail()
            .FinalizeAndGetContent();
}