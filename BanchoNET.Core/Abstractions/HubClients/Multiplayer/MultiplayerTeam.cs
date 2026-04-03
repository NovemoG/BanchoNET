using MessagePack;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer;

[Serializable]
[MessagePackObject]
public class MultiplayerTeam
{
    [Key(0)]
    public int ID { get; set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;
}