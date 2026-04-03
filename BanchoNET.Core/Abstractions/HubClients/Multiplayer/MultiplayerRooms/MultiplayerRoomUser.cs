using System.Text.Json.Serialization;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.Match;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Api.Player;
using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.MultiplayerRooms;

[Serializable]
[MessagePackObject]
[method: JsonConstructor]
public class MultiplayerRoomUser(int userId) : IEquatable<MultiplayerRoomUser>
{
    [Key(0)]
    public readonly int UserID = userId;

    [Key(1)]
    public MultiplayerUserState State { get; set; } = MultiplayerUserState.Idle;
    
    [Key(2)]
    public BeatmapAvailability BeatmapAvailability { get; set; } = BeatmapAvailability.Unknown();
    
    [Key(3)]
    public IEnumerable<ApiMod> Mods { get; set; } = [];

    [Key(4)]
    public MatchUserState? MatchState { get; set; }
    
    [Key(5)]
    public int? RulesetId;
    
    [Key(6)]
    public int? BeatmapId;
    
    [Key(7)]
    public bool VotedToSkipIntro;
    
    [Key(8)]
    public MultiplayerRoomUserRole Role;

    [IgnoreMember]
    public ApiPlayer? User { get; set; }
    
    public bool CanStartGameplay()
    {
        switch (State)
        {
            case MultiplayerUserState.Loaded:
            case MultiplayerUserState.ReadyForGameplay:
                return true;

            default:
                return false;
        }
    }
    
    public bool Equals(MultiplayerRoomUser? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other == null) return false;

        return UserID == other.UserID;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj?.GetType() != GetType()) return false;

        return Equals((MultiplayerRoomUser)obj);
    }

    public override int GetHashCode() => UserID.GetHashCode();
}