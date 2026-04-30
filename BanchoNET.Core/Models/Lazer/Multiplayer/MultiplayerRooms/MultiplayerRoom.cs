namespace BanchoNET.Core.Models.Lazer.Multiplayer.MultiplayerRooms;

public class MultiplayerRoom : Room
{
    public MultiplayerSettings Settings { get; set; } = new();
}