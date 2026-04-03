using MatchType = BanchoNET.Core.Abstractions.HubClients.Multiplayer.Match.MatchType;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.MultiplayerRooms;

public class Room
{
    public long RoomId { get; set; }
    public string Name { get; set; } = "Room";
    public string? Password { get; set; }
    public MatchType Type { get; set; }
    public QueueMode QueueMode { get; set; }
    public TimeSpan AutoStartDuration { get; set; }
    public bool AutoSkip { get; set; }
    
}