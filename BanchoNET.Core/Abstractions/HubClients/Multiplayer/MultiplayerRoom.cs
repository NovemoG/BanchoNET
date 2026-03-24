namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer;

public class MultiplayerRoom : Room
{
    public MultiplayerSettings Settings { get; set; } = new();
}