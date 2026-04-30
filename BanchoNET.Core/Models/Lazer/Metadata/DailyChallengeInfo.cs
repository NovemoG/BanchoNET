using MessagePack;

namespace BanchoNET.Core.Models.Lazer.Metadata;

[Serializable]
[MessagePackObject]
public struct DailyChallengeInfo
{
    [Key(0)]
    public long RoomID { get; set; }
}