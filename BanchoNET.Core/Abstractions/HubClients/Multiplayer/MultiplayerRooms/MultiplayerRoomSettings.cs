using MessagePack;
using MatchType = BanchoNET.Core.Abstractions.HubClients.Multiplayer.Match.MatchType;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.MultiplayerRooms;

[Serializable]
[MessagePackObject]
public class MultiplayerRoomSettings
{
    [Key(0)]
    public string Name { get; set; } = "Unnamed room";

    [Key(1)]
    public long PlaylistItemId { get; set; }

    [Key(2)]
    public string Password { get; set; } = string.Empty;

    [Key(3)]
    public MatchType MatchType { get; set; } = MatchType.HeadToHead;

    [Key(4)]
    public QueueMode QueueMode { get; set; } = QueueMode.HostOnly;

    [Key(5)]
    public TimeSpan AutoStartDuration { get; set; }

    [Key(6)]
    public bool AutoSkip { get; set; }

    [IgnoreMember]
    public bool AutoStartEnabled => AutoStartDuration != TimeSpan.Zero;
    
    public MultiplayerRoomSettings()
    {
    }

    public MultiplayerRoomSettings(Room room)
    {
        Name = room.Name;
        Password = room.Password ?? string.Empty;
        MatchType = room.Type;
        QueueMode = room.QueueMode;
        AutoStartDuration = room.AutoStartDuration;
        AutoSkip = room.AutoSkip;
    }
}