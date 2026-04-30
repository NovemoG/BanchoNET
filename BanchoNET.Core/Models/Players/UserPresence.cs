using BanchoNET.Core.Models.Lazer.Metadata;
using MessagePack;

namespace BanchoNET.Core.Models.Players;

[Serializable]
[MessagePackObject]
public struct UserPresence
{
    [Key(0)]
    public UserActivity? Activity { get; set; }
    
    [Key(1)]
    public UserStatus? Status { get; set; }
}