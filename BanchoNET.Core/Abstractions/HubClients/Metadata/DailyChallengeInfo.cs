using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Metadata;

[Serializable]
[MessagePackObject]
public struct DailyChallengeInfo
{
    [Key(0)]
    public long RoomID { get; set; }
}