using MatchType = BanchoNET.Core.Abstractions.HubClients.Multiplayer.Match.MatchType;

namespace BanchoNET.Core.Abstractions.HubClients.Multiplayer;

public class MultiplayerSettings
{
    public MatchType MatchType { get; set; }
}