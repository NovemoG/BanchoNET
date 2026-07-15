using System.Text.Json.Serialization;
using BanchoNET.Core.Models.Lazer.Multiplayer.Match;
using MessagePack;

namespace BanchoNET.Core.Models.Lazer.Multiplayer.MultiplayerRooms;

[Serializable]
[MessagePackObject]
public class MultiplayerRoom
{
    [Key(0)]
    public readonly long RoomID;
    
    [Key(1)]
    public MultiplayerRoomState State { get; set; }
    
    [Key(2)]
    public MultiplayerRoomSettings Settings { get; set; } = new();
    
    [Key(3)]
    public IList<MultiplayerRoomUser> Users { get; set; } = new List<MultiplayerRoomUser>();
    
    [Key(4)]
    public MultiplayerRoomUser? Host { get; set; }

    [Key(5)]
    public MatchRoomState? MatchState { get; set; }

    [Key(6)]
    public IList<MultiplayerPlaylistItem> Playlist { get; set; } = new List<MultiplayerPlaylistItem>();
    
    [Key(7)]
    public IList<MultiplayerCountdown> ActiveCountdowns { get; set; } = new List<MultiplayerCountdown>();
    
    [Key(8)]
    public int ChannelID { get; set; }

    [JsonConstructor]
    [SerializationConstructor]
    public MultiplayerRoom(long roomId)
    {
        RoomID = roomId;
    }
}