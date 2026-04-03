namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer.MultiplayerRooms;

public class MultiplayerRoom : Room
{
    public MultiplayerSettings Settings { get; set; } = new();
}